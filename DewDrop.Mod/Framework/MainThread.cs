using System.Collections.Concurrent;

namespace DewDrop.Framework
{
    /// <summary>
    /// Network and disk work runs on the thread pool; anything touching game state or textures is posted back
    /// here and run on the next update tick.
    /// </summary>
    internal static class MainThread
    {
        private static readonly ConcurrentQueue<Action> _queue = new();

        public static void Post(Action action) => _queue.Enqueue(action);

        public static void Pump()
        {
            while (_queue.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    ModEntry.Log.Error($"Error in a queued action: {ex}");
                }
            }
        }

        /// <summary>Runs a function on the main thread at the next tick and hands its result back to a background task.</summary>
        public static Task<T> Invoke<T>(Func<T> func)
        {
            // Continuations run on the thread pool, so the caller's remaining work never lands on the main thread
            var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.Enqueue(() =>
            {
                try
                {
                    done.SetResult(func());
                }
                catch (Exception ex)
                {
                    done.SetException(ex);
                }
            });
            return done.Task;
        }

        /// <summary>Runs work in the background and hands the result (or error) back on the main thread.</summary>
        public static void Run<T>(Func<Task<T>> work, Action<T> onDone, Action<Exception>? onError = null)
        {
            Task.Run(async () =>
            {
                try
                {
                    var result = await work();
                    Post(() => onDone(result));
                }
                catch (Exception ex)
                {
                    ModEntry.Log.Trace($"Background task failed: {ex}");
                    Post(() => (onError ?? (e => ModEntry.Toast(e.Message, error: true)))(ex));
                }
            });
        }
    }
}
