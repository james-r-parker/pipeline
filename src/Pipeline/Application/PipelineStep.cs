namespace Pipeline;

internal interface IPipelineStep : IDisposable
{
        bool IsRunning { get; }
        string Name { get; set; }
        Func<PipelineRequest, Task> Next { get; set; }
        CancellationToken CancellationToken { get; set; }
        Task Invoke(PipelineRequest request);
        Task Start();
}

internal interface IPipelineSource : IPipelineStep
{
}

public abstract class PipelineSource : PipelineBufferedStep, IPipelineSource
{
        protected PipelineSource(IOptionsMonitor<PipelineOptions> settings) : base(settings)
        {
        }
}

public abstract class PipelineStep : IPipelineStep
{
        public virtual bool IsRunning { get; } = false;
        public string Name { get; set; } = string.Empty;
        public Func<PipelineRequest, Task> Next { get; set; } = (r) => Task.CompletedTask;
        public CancellationToken CancellationToken { get; set; }

        protected virtual Task Process(PipelineRequest request)
        {
                return Task.CompletedTask;
        }

        public virtual async Task Invoke(PipelineRequest request)
        {
                try
                {
                        await Process(request);
                }
                catch (Exception ex)
                {
                        request.Item.AddError(Name, ex);
                }

                await Next(request);
        }

        public virtual Task Start() => Task.CompletedTask;

        public virtual void Dispose() { }
}

internal sealed class PipelineInlineStep : PipelineStep
{
        private readonly Func<PipelineRequest, Task> _inline;

        public PipelineInlineStep(Func<PipelineRequest, Task> inline)
        {
                _inline = inline;
        }

        protected override Task Process(PipelineRequest request)
        {
                return _inline(request);
        }
}

public abstract class PipelineBufferedStep : PipelineStep
{
        private bool isDisposed = false;
        private Task? _worker;
        private readonly ConcurrentQueue<PipelineRequest> _buffer;
        private readonly SemaphoreSlim _concurrency;
        private readonly IOptionsMonitor<PipelineOptions> _settings;

        public override bool IsRunning => _buffer.Count > 0 || _concurrency.CurrentCount < _settings.CurrentValue.MaxStepConcurrency;

        public PipelineBufferedStep(IOptionsMonitor<PipelineOptions> settings)
        {
                _buffer = new ConcurrentQueue<PipelineRequest>();
                _concurrency = new SemaphoreSlim(settings.CurrentValue.MaxStepConcurrency, settings.CurrentValue.MaxStepConcurrency);
                _settings = settings;
        }

        public override Task Start()
        {
                _worker = Task.Run(Process, CancellationToken);
                return Task.CompletedTask;
        }

        public override async void Dispose()
        {
                isDisposed = true;
                if (_worker != null)
                {
                        await _worker;
                        _worker.Dispose();
                }
                _concurrency.Dispose();
                _buffer.Clear();
                base.Dispose();
        }

        public override async Task Invoke(PipelineRequest request)
        {
                while (_buffer.Count >= _settings.CurrentValue.MaxStepQueueSize)
                {
                        await Task.Delay(_settings.CurrentValue.Wait, CancellationToken);
                }

                _buffer.Enqueue(request);
        }

        protected override abstract Task Process(PipelineRequest request);

        private async Task Process()
        {
                while (!CancellationToken.IsCancellationRequested && !isDisposed)
                {
                        if (_buffer.Count == 0)
                        {
                                await Task.Delay(_settings.CurrentValue.Wait, CancellationToken);
                                continue;
                        }

                        await _concurrency.WaitAsync(CancellationToken);

                        if (_buffer.TryDequeue(out PipelineRequest? request))
                        {
                                _ = Task.Run(async () =>
                                {
                                        try
                                        {
                                                try
                                                {
                                                        await Process(request);
                                                }
                                                catch (Exception ex)
                                                {
                                                        request.Item.AddError(Name, ex);
                                                }

                                                await Next(request);
                                        }
                                        finally
                                        {
                                                _concurrency.Release();
                                        }

                                }, CancellationToken);
                        }

                }
        }
}

internal sealed class PipelineInlineBufferedStep : PipelineBufferedStep
{
        private readonly Func<PipelineRequest, Task> _inline;

        public PipelineInlineBufferedStep(
            Func<PipelineRequest, Task> inline,
            IOptionsMonitor<PipelineOptions> options)
            : base(options)
        {
                _inline = inline;
        }

        protected override Task Process(PipelineRequest request)
        {
                return _inline(request);
        }
}