namespace DotNetHelp.Pipelines;

public sealed class Pipeline : IDisposable
{
        private bool _isRunning;
        private bool _isFinalised;

        private readonly PipelineResult _result;
        private readonly Context _global;
        private readonly IList<IPipelineSource> _sources;
        private readonly IList<IPipelineStep> _steps;
        private readonly CancellationTokenSource _cancellationToken;
        private readonly IServiceProvider _serviceProvider;

        internal Pipeline(
            Context globalContext,
            IList<IPipelineSource> sources,
            IList<IPipelineStep> steps,
            IServiceProvider serviceProvider,
            CancellationTokenSource cancellationToken)
        {
                _isRunning = false;
                _isFinalised = false;
                _sources = sources;
                _steps = steps;
                _global = globalContext;
                _cancellationToken = cancellationToken;
                _serviceProvider = serviceProvider;
                _result = new PipelineResult(cancellationToken.Token);
        }

        public IAsyncEnumerable<Context> Result => _result;

        public Context GlobalContext => _global;

        public bool IsRunning => _isRunning;

        internal async Task<PipelineRequest> AddInputRequest(PipelineRequest request)
        {
                if (_sources.Count > 0)
                {
                        foreach (var source in _sources)
                        {
                                await source.Invoke(request);
                        }
                }
                else if (_steps.Count > 0)
                {
                        await _steps[0].Invoke(request);
                }

                return request;
        }

        public Task<PipelineRequest> AddInput(Context input)
        {
                var request = new PipelineRequest(_global, input, _serviceProvider.CreateScope().ServiceProvider);
                return AddInputRequest(request);
        }

        public Task<PipelineRequest> AddInput<T>(T input)
                where T : class
        {
                var request = new Context();
                request.Add<T>(input);
                return AddInput(request);
        }

        public async Task Finalise()
        {
                while (_sources.Any(x => !x.GetType().IsAssignableTo(typeof(IFinalisablePipelineStep)) && x.IsRunning))
                {
                        await Task.Delay(5, _cancellationToken.Token);
                }

                foreach (IFinalisablePipelineStep source in _sources.Where(x => x.GetType().IsAssignableTo(typeof(IFinalisablePipelineStep))))
                {
                        await source.Finalise();
                }

                foreach (var step in _steps)
                {
                        if (step is IFinalisablePipelineStep finalisable)
                        {
                                await finalisable.Finalise();
                        }
                        else
                        {
                                while (step.IsRunning)
                                {
                                        await Task.Delay(5, _cancellationToken.Token);
                                }
                        }
                }

                _isFinalised = true;
        }

        public async Task Wait()
        {
                while (_isRunning)
                {
                        await Task.Delay(10, _cancellationToken.Token);
                }
        }

        public async Task<Context?> InvokeSync<T>(T input)
                where T : class
        {
                await Start();

                await AddInput<T>(input);

                await Finalise();

                var output = new List<Context>();
                await foreach (var item in Result.WithCancellation(_cancellationToken.Token))
                {
                        output.Add(item);
                        break;
                }

                return output.FirstOrDefault();
        }

        public async Task<IList<Context>> InvokeManySync<T>(IEnumerable<T> inputs, int? maxThreads = null)
                where T : class
        {
                var task = await Start();

                var max = maxThreads.HasValue ? maxThreads.Value : Environment.ProcessorCount;
                var tasks = new List<Task>();

                using (var concurrency = new SemaphoreSlim(max, max))
                {
                        foreach (var input in inputs)
                        {
                                await concurrency.WaitAsync();
                                tasks.Add(Task.Run(async () =>
                                {
                                        try
                                        {
                                                await AddInput<T>(input);
                                        }
                                        finally
                                        {
                                                concurrency.Release();
                                        }
                                }, _cancellationToken.Token));
                        }

                        await Task.WhenAll(tasks);
                }

                await Finalise();

                var output = new List<Context>();
                await foreach (var item in Result.WithCancellation(_cancellationToken.Token))
                {
                        output.Add(item);
                }
                return output;
        }

        public async Task<Context?> Invoke<T>(T input)
                where T : class
        {
                await Start();

                await AddInput<T>(input);

                await Finalise();

                await foreach (var item in Result.WithCancellation(_cancellationToken.Token))
                {
                        return item;
                }

                return null;
        }

        public async IAsyncEnumerable<Context> InvokeMany<T>(IEnumerable<T> inputs)
                where T : class
        {
                var task = await Start();

                foreach (var input in inputs)
                {
                        await AddInput<T>(input);
                }

                await Finalise();

                await task;

                await foreach (var item in Result.WithCancellation(_cancellationToken.Token))
                {
                        yield return item;
                }
        }

        public async Task<Task> Start()
        {
                if (_isRunning)
                {
                        throw new PipelineException("Pipeline already running");
                }

                _isRunning = true;
                _result.Start();

                foreach (var step in _steps)
                {
                        await step.Start();
                }

                foreach (var source in _sources)
                {
                        await source.Start();
                }

                var task = Task.Run(async () =>
                {
                        while (!_isFinalised)
                        {
                                await Task.Delay(5, _cancellationToken.Token);
                        }

                        while (_sources.Any(x => x.IsRunning))
                        {
                                await Task.Delay(5, _cancellationToken.Token);
                        }

                        while (_steps.Any(x => x.IsRunning))
                        {
                                await Task.Delay(5, _cancellationToken.Token);
                        }

                        _result.Stop();
                        _isRunning = false;
                });

                return task;
        }

        public void Dispose()
        {
                _result.DisposeAsync();

                foreach (var source in _sources)
                {
                        source.Dispose();
                }

                foreach (var step in _steps)
                {
                        step.Dispose();
                }
        }

        internal Task AddOutput(Context output)
        {
                _result.Add(output);
                return Task.CompletedTask;
        }
}