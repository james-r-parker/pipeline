namespace Pipeline.Application;

internal class PipelineResult : IAsyncEnumerator<Context>, IAsyncEnumerable<Context>
{
        private bool _isRunning;

        private readonly ConcurrentQueue<Context> _output;
        private readonly CancellationToken _cancellationToken;

        internal PipelineResult(CancellationToken cancellationToken)
        {
                _output = new ConcurrentQueue<Context>();
                _isRunning = false;
                _cancellationToken = cancellationToken;
        }

        public void Start()
        {
                _isRunning = true;
        }

        public void Stop()
        {
                _isRunning = false;
        }

        public Context Current { get; set; } = new Context();

        public void Add(Context item) => _output.Enqueue(item);

        public async ValueTask<bool> MoveNextAsync()
        {
                if (!_isRunning && _output.Count == 0)
                {
                        return false;
                }

                if (_output.Count > 0 && _output.TryDequeue(out Context? i))
                {
                        Current = i;
                        return true;
                }

                while (_isRunning && _output.Count == 0)
                {
                        await Task.Delay(5, _cancellationToken);
                }

                if (!_isRunning && _output.Count == 0)
                {
                        return false;
                }

                if (_output.Count > 0 && _output.TryDequeue(out Context? ii))
                {
                        Current = ii;
                        return true;
                }

                return false;
        }

        public ValueTask DisposeAsync()
        {
                _output.Clear();
                return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerator<Context> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
                while (await MoveNextAsync())
                {
                        yield return Current;
                }
        }
}
