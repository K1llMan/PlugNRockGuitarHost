using System.Collections.Concurrent;

namespace PlugNRockGuitarHost.Server
{
    public class StaDispatcher : IDisposable
    {
        private readonly BlockingCollection<Action> queue = new();
        private readonly Thread thread;

        public StaDispatcher()
        {
            thread = new Thread(() => {
                foreach (Action action in queue.GetConsumingEnumerable())
                    action();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        // Аналог PostAsync с возвратом результата
        public Task<T> InvokeAsync<T>(Func<T> func)
        {
            TaskCompletionSource<T> tcs = new();
            queue.Add(() =>
            {
                try { tcs.SetResult(func()); }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            return tcs.Task;
        }

        // Перегрузка для методов без возвращаемого значения
        public Task InvokeAsync(Action action) => InvokeAsync(() => { action(); return true; });

        public void Dispose() => queue.CompleteAdding();
    }
}