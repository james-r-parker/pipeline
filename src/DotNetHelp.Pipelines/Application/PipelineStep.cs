namespace DotNetHelp.Pipelines;

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

internal interface IFinalisablePipelineStep
{
        void Finalise();
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

        public Context StepContext { get; set; } = new Context();

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

public abstract class PipelineFilterStep : PipelineStep
{
        protected abstract Task<bool> Filter(PipelineRequest request);

        public override async Task Invoke(PipelineRequest request)
        {
                var runNext = false;

                try
                {
                        runNext = await Filter(request);
                }
                catch (Exception ex)
                {
                        request.Item.AddError(Name, ex);
                }

                if (runNext)
                {
                        await Next(request);
                }
        }
}

internal sealed class PipelineInlineFilterStep : PipelineFilterStep
{
        private readonly Func<PipelineRequest, Task<bool>> _inline;

        public PipelineInlineFilterStep(Func<PipelineRequest, Task<bool>> inline)
        {
                _inline = inline;
        }

        protected override Task<bool> Filter(PipelineRequest request)
        {
                return _inline(request);
        }
}

public abstract class PipelineBranchStep : PipelineStep, IFinalisablePipelineStep
{
        private readonly PipelineBuilder _builder;
        private Pipeline _pipeline;

        public PipelineBranchStep(PipelineBuilder builder)
        {
                _builder = builder;
        }

        public override bool IsRunning => _pipeline.IsRunning;

        public abstract Task<bool> Filter(PipelineRequest request);

        public void Finalise()
        {
                _pipeline.Finalise();
        }

        public override async Task Invoke(PipelineRequest request)
        {
                var runNext = false;

                try
                {
                        runNext = await Filter(request);
                }
                catch (Exception ex)
                {
                        request.Item.AddError(Name, ex);
                }

                if (runNext)
                {
                        await _pipeline.AddInput(request.Item);
                }
                else
                {
                        await Next(request);
                }
        }

        public override void Dispose()
        {
                _pipeline.Dispose();
        }

        public override async Task Start()
        {
                _pipeline = _builder.Build(CancellationToken, Next);
                await _pipeline.Invoke();
        }
}

internal sealed class PipelineInlineBranchStep : PipelineBranchStep
{
        private readonly Func<PipelineRequest, Task<bool>> _filter;

        public PipelineInlineBranchStep(PipelineBuilder builder, Func<PipelineRequest, Task<bool>> filter)
                : base(builder)
        {
                _filter = filter;
        }

        public override Task<bool> Filter(PipelineRequest request)
        {
                return _filter(request);
        }
}

public abstract class PipelineForkStep : PipelineStep, IFinalisablePipelineStep
{
        private readonly PipelineBuilder _builder;
        private Pipeline _pipeline;

        public PipelineForkStep(PipelineBuilder builder)
        {
                _builder = builder;
        }

        public Func<PipelineRequest, Task> End { get; set; }

        public override bool IsRunning => _pipeline.IsRunning;

        public abstract Task<bool> Filter(PipelineRequest request);

        public void Finalise()
        {
                _pipeline.Finalise();
        }

        public override async Task Invoke(PipelineRequest request)
        {
                var runNext = false;

                try
                {
                        runNext = await Filter(request);
                }
                catch (Exception ex)
                {
                        request.Item.AddError(Name, ex);
                }

                if (runNext)
                {
                        await _pipeline.AddInput(request.Item);
                }
                else
                {
                        await Next(request);
                }
        }

        public override void Dispose()
        {
                _pipeline.Dispose();
        }

        public override async Task Start()
        {
                _pipeline = _builder.Build(CancellationToken, End);
                await _pipeline.Invoke();
        }
}

internal sealed class PipelineInlineForkStep : PipelineForkStep
{
        private readonly Func<PipelineRequest, Task<bool>> _filter;

        public PipelineInlineForkStep(PipelineBuilder builder, Func<PipelineRequest, Task<bool>> filter)
                : base(builder)
        {
                _filter = filter;
        }

        public override Task<bool> Filter(PipelineRequest request)
        {
                return _filter(request);
        }
}