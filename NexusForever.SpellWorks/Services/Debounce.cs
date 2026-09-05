namespace NexusForever.SpellWorks.Services
{
    /// <summary>
    /// One piece of deferred work, superseded by the next call.
    /// </summary>
    public sealed class Debounce : IDisposable
    {
        private readonly TimeSpan _delay;

        private CancellationTokenSource _cancellation;

        public Debounce(TimeSpan delay)
        {
            _delay = delay;
        }

        /// <summary>
        /// Wait out the delay, run <paramref name="work"/>, then hand its result to
        /// <paramref name="commit"/> - unless a newer call has superseded this one by then.
        /// </summary>
        public async Task Run<T>(Func<CancellationToken, Task<T>> work, Func<T, Task> commit)
        {
            Cancel();
            _cancellation = new CancellationTokenSource();
            CancellationToken token = _cancellation.Token;

            try
            {
                await Task.Delay(_delay, token);

                T result = await work(token);

                // The work ran to completion but a newer call started while it did; committing now would
                // put a stale answer on screen and leave it there.
                if (token.IsCancellationRequested)
                    return;

                await commit(result);
            }
            catch (TaskCanceledException)
            {
            }
        }

        /// <summary>As <see cref="Run{T}(Func{CancellationToken, Task{T}}, Func{T, Task})"/>, committing synchronously.</summary>
        public Task Run<T>(Func<CancellationToken, Task<T>> work, Action<T> commit)
        {
            return Run(work, result =>
            {
                commit(result);
                return Task.CompletedTask;
            });
        }

        /// <summary>Wait out the delay, then commit. For deferred work with nothing to hand over.</summary>
        public Task Run(Func<Task> commit) => Run(_ => Task.FromResult(0), _ => commit());

        /// <summary>Abandon whatever is in flight. The next <c>Run</c> does this for itself.</summary>
        public void Cancel() => _cancellation?.Cancel();

        public void Dispose() => Cancel();
    }
}
