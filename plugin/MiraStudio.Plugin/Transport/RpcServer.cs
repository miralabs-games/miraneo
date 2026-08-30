using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using MiraStudio.Core;

namespace MiraStudio.Transport
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
                _log.LogError("MiraStudio RPC failed to bind " + prefix + ": " + e.Message);
                _log.LogError("If this is an access/ACL error, run once from an elevated prompt: netsh http add urlacl url=" + prefix + " user=" + Environment.UserName);
                _listener = null;
                return false;
            }
            _running = true;
            _thread = new Thread(AcceptLoop) { IsBackground = true, Name = "MiraStudio.Rpc" };
            _thread.Start();
            _log.LogInfo("MiraStudio RPC listening on " + prefix);
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
                _log.LogError("MiraStudio RPC request failed: " + e);
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
