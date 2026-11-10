using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Miraneo.Transport
{
    // Bridges the RPC worker threads to the Unity main thread: work is queued
    // here and drained from the plugin's Update() every frame.
    public sealed class MainThreadDispatcher
    {
        private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        public void Post(Action action) => _queue.Enqueue(action);

        public T Run<T>(Func<T> fn, int timeoutMs)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.Enqueue(() =>
            {
                try { tcs.TrySetResult(fn()); }
                catch (Exception e) { tcs.TrySetException(e); }
            });
            if (!tcs.Task.Wait(timeoutMs))
                throw new TimeoutException("Unity main thread did not respond within " + timeoutMs + " ms (is Studio running and not stalled?)");
            return tcs.Task.Result;
        }

        public void Drain()
        {
            Action action;
            while (_queue.TryDequeue(out action))
            {
                try { action(); }
                catch { }
            }
        }
    }
}
