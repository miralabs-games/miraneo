using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using Miraneo.Core;

namespace Miraneo.Transport
{
    // Local JSON-RPC 2.0 endpoint: POST /rpc, GET /health. Requests are received
    // on worker threads and executed on the Unity main thread via the dispatcher.
    public sealed class RpcServer : IDisposable
    {
        private sealed class Method
        {
            public Func<JsonObject, Task<JsonNode>> Invoke;
            public int TimeoutMs;
        }

        private readonly ManualLogSource _log;
        private readonly MainThreadDispatcher _dispatcher;
        private readonly Func<JsonNode> _health;
        private readonly Dictionary<string, Method> _methods = new Dictionary<string, Method>();
        private HttpListener _listener;
        private Thread _thread;
        private volatile bool _running;

        public RpcServer(ManualLogSource log, MainThreadDispatcher dispatcher, Func<JsonNode> health)
        {
            _log = log;
            _dispatcher = dispatcher;
            _health = health;
        }

        public void Register(string name, Func<JsonObject, JsonNode> handler, int timeoutMs = 15000)
        {
            _methods[name] = new Method
            {
                TimeoutMs = timeoutMs,
                Invoke = p => Task.FromResult(_dispatcher.Run(() => handler(p), timeoutMs)),
            };
        }

        // The starter runs on the main thread and must return quickly with a task
        // that completes later (e.g. a coroutine finishing a screenshot).
        public void RegisterAsync(string name, Func<JsonObject, Task<JsonNode>> starter, int timeoutMs = 60000)
        {
            _methods[name] = new Method
            {
                TimeoutMs = timeoutMs,
                Invoke = p => _dispatcher.Run(() => starter(p), 15000),
            };
        }

        // Deliberately not routed through the dispatcher, unlike Register: every step
        // marshals itself, so running this loop inside Drain() would have the main
        // thread waiting on a queue only it can drain. The loop stays on the HTTP
        // worker thread and blocks there instead.
        public void RegisterBatch(string name, int maxSteps, int timeoutMs = 300000)
        {
            _methods[name] = new Method
            {
                TimeoutMs = timeoutMs,
                Invoke = p => Task.Run(() => RunBatch(name, p, maxSteps)),
            };
        }

        private JsonNode RunBatch(string self, JsonObject p, int maxSteps)
        {
            var steps = p.Get("steps") as JsonArray;
            if (steps == null || steps.Count == 0)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "parameter 'steps' must be a non-empty array of {method, params}");
            if (steps.Count > maxSteps)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "a batch takes at most " + maxSteps + " steps, got " + steps.Count);

            // Nothing runs until every step is known to be callable: a batch has no
            // rollback, so a typo in the last step must not leave the first ones applied.
            var names = new string[steps.Count];
            var args = new JsonObject[steps.Count];
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i] as JsonObject;
                if (step == null)
                    throw new StudioApiException(StudioApiException.InvalidParams, "step " + i + " must be an object {method, params}");
                var method = step.GetString("method");
                if (string.IsNullOrEmpty(method))
                    throw new StudioApiException(StudioApiException.InvalidParams, "step " + i + " is missing 'method'");
                if (method == self)
                    throw new StudioApiException(StudioApiException.InvalidParams, "step " + i + ": batches cannot be nested");
                if (!_methods.ContainsKey(method))
                    throw new StudioApiException(StudioApiException.InvalidParams, "step " + i + ": unknown method '" + method + "'");
                // A screenshot returns its image as base64 in the JSON body; inside a
                // batch it would be dumped as text no client can render.
                if (method == "studio_screenshot")
                    throw new StudioApiException(StudioApiException.InvalidParams,
                        "step " + i + ": studio_screenshot cannot run in a batch, call it on its own so the image comes back as an image");
                var prm = step.Get("params");
                if (prm != null && !(prm is JsonNull) && !(prm is JsonObject))
                    throw new StudioApiException(StudioApiException.InvalidParams, "step " + i + ": 'params' must be an object");
                names[i] = method;
                args[i] = prm as JsonObject ?? new JsonObject();
            }

            var results = new JsonArray();
            for (var i = 0; i < names.Length; i++)
            {
                var method = _methods[names[i]];
                JsonNode result;
                try
                {
                    var task = method.Invoke(args[i]);
                    if (!task.Wait(method.TimeoutMs))
                        throw new TimeoutException("operation timed out after " + method.TimeoutMs + " ms");
                    result = task.Result;
                }
                catch (Exception e)
                {
                    var inner = Unwrap(e);
                    var api = inner as StudioApiException;
                    var code = api != null ? api.Code : inner is TimeoutException ? -32002 : -32603;
                    _log.LogWarning("Miraneo batch stopped at step " + i + " (" + names[i] + "): " + inner.Message);
                    return new JsonObject()
                        .Add("ok", false)
                        .Add("executed", i)
                        .Add("stopped_at", i)
                        .Add("error", new JsonObject()
                            .Add("step", i)
                            .Add("method", names[i])
                            .Add("code", code)
                            .Add("message", inner.Message))
                        .Add("steps", results);
                }
                results.Add(new JsonObject().Add("step", i).Add("method", names[i]).Add("result", result));
            }

            return new JsonObject()
                .Add("ok", true)
                .Add("executed", names.Length)
                .Add("stopped_at", JsonNull.Instance)
                .Add("error", JsonNull.Instance)
                .Add("steps", results);
        }

        public bool Start(int port)
        {
            var prefix = "http://localhost:" + port + "/";
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(prefix);
                _listener.Start();
            }
            catch (Exception e)
            {
                _log.LogError("Miraneo RPC failed to bind " + prefix + ": " + e.Message);
                _log.LogError("If this is an access/ACL error, run once from an elevated prompt: netsh http add urlacl url=" + prefix + " user=" + Environment.UserName);
                _listener = null;
                return false;
            }
            _running = true;
            _thread = new Thread(AcceptLoop) { IsBackground = true, Name = "Miraneo.Rpc" };
            _thread.Start();
            _log.LogInfo("Miraneo RPC listening on " + prefix);
            return true;
        }

        private void AcceptLoop()
        {
            while (_running)
            {
                HttpListenerContext ctx;
                try { ctx = _listener.GetContext(); }
                catch
                {
                    if (_running) continue;
                    break;
                }
                ThreadPool.QueueUserWorkItem(_ => Handle(ctx));
            }
        }

        private void Handle(HttpListenerContext ctx)
        {
            try
            {
                var req = ctx.Request;
                if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/health")
                {
                    WriteJson(ctx, 200, _health());
                    return;
                }
                if (req.HttpMethod != "POST" || req.Url.AbsolutePath != "/rpc")
                {
                    WriteJson(ctx, 404, RpcError(JsonNull.Instance, -32600, "use POST /rpc or GET /health"));
                    return;
                }

                string body;
                using (var reader = new StreamReader(req.InputStream, Encoding.UTF8))
                    body = reader.ReadToEnd();

                JsonNode parsed;
                try { parsed = Json.Parse(body); }
                catch (Exception e)
                {
                    WriteJson(ctx, 200, RpcError(JsonNull.Instance, -32700, "parse error: " + e.Message));
                    return;
                }

                var envelope = parsed as JsonObject;
                if (envelope == null)
                {
                    WriteJson(ctx, 200, RpcError(JsonNull.Instance, -32600, "request must be a JSON object"));
                    return;
                }

                var id = envelope.Get("id") ?? JsonNull.Instance;
                var methodName = envelope.GetString("method");
                if (methodName == null)
                {
                    WriteJson(ctx, 200, RpcError(id, -32600, "missing method"));
                    return;
                }

                Method method;
                if (!_methods.TryGetValue(methodName, out method))
                {
                    WriteJson(ctx, 200, RpcError(id, -32601, "unknown method: " + methodName));
                    return;
                }

                var prm = envelope.Get("params") as JsonObject ?? new JsonObject();
                JsonNode result;
                try
                {
                    var task = method.Invoke(prm);
                    if (!task.Wait(method.TimeoutMs))
                        throw new TimeoutException("operation timed out after " + method.TimeoutMs + " ms");
                    result = task.Result;
                }
                catch (Exception e)
                {
                    var inner = Unwrap(e);
                    var api = inner as StudioApiException;
                    var code = api != null ? api.Code : inner is TimeoutException ? -32002 : -32603;
                    WriteJson(ctx, 200, RpcError(id, code, inner.Message));
                    return;
                }

                WriteJson(ctx, 200, new JsonObject().Add("jsonrpc", "2.0").Add("id", id).Add("result", result));
            }
            catch (Exception e)
            {
                _log.LogError("Miraneo RPC request failed: " + e);
                try { ctx.Response.Abort(); } catch { }
            }
        }

        private static Exception Unwrap(Exception e)
        {
            var ae = e as AggregateException;
            while (ae != null && ae.InnerException != null)
            {
                e = ae.InnerException;
                ae = e as AggregateException;
            }
            return e;
        }

        private static void WriteJson(HttpListenerContext ctx, int status, JsonNode node)
        {
            var bytes = Encoding.UTF8.GetBytes(node.ToString());
            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = "application/json";
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.OutputStream.Close();
        }

        private static JsonObject RpcError(JsonNode id, int code, string message) => new JsonObject()
            .Add("jsonrpc", "2.0")
            .Add("id", id)
            .Add("error", new JsonObject().Add("code", code).Add("message", message));

        public void Dispose()
        {
            _running = false;
            try
            {
                if (_listener != null)
                {
                    _listener.Stop();
                    _listener.Close();
                }
            }
            catch { }
        }
    }
}
