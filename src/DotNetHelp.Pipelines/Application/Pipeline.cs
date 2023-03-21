namespace DotNetHelp.Pipelines;

/// <summary>
/// A built pipeline that can execute requests.
/// </summary>
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

        /// <summary>
        /// The results from the output of the pipeline.
        /// </summary>
        public IAsyncEnumerable<Context> Result => _result;

        /// <summary>
        /// The global context of the pipeline shared between all requests.
        /// </summary>
        public Context GlobalContext => _global;

        /// <summary>
        /// True if the pipeline is running otherwise false.
        /// </summary>
        public bool IsRunning => _isRunning;

        internal async Task<PipelineRequest> AddInputRequest(PipelineRequest request)
        {
                if (!_isRunning)
                {
                        throw new PipelineException("Can not add input into a pipeline that is not running, make sure you have started the pipeline first.");
                }

                if (_isFinalised)
                {
                        throw new PipelineException("Can not add input into a pipeline that has being finalised.");
                }

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

        /// <summary>
        /// Executes the pipeline by creating using the context supplied.
        /// If the pipeline has no buffered steps the return of this method will be the result of the 
        /// pipeline execution.
        /// </summary>
        /// <param name="input">The context used for the request.</param>
        /// <returns><see cref="PipelineRequest"/></returns>
        public Task<PipelineRequest> AddInput(Context input)
        {
                var request = new PipelineRequest(_global, input, _serviceProvider.CreateScope().ServiceProvider);
                return AddInputRequest(request);
        }

        /// <summary>
        /// Executes the pipeline by creating a new context and adding the value T to the context.
        /// If the pipeline has no buffered steps the return of this method will be the result of the 
        /// pipeline execution.
        /// </summary>
        /// <typeparam name="T">The type of the item to add to the context of the request.</typeparam>
        /// <param name="input">The object to include in the context of the request.</param>
        /// <returns><see cref="PipelineRequest"/></returns>
        public Task<PipelineRequest> AddInput<T>(T input)
                where T : class
        {
                var request = new Context();
                request.Add<T>(input);
                return AddInput(request);
        }

        /// <summary>
        /// Notify the pipeline that no further inputs will be supplied. This allows the pipeline to complete.
        /// </summary>
        /// <returns><see cref="Task"/></returns>
        public async Task Finalise()
        {
                if (!_isRunning)
                {
                        throw new PipelineException("Can not finalise a pipeline that is not running");
                }

                if (_isFinalised)
                {
                        throw new PipelineException("Pipeline already finalised");
                }

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

        /// <summary>
        /// Wait for the pipeline to finish, this will only complete once the pipeline is finalised <see cref="Finalise" />
        /// </summary>
        /// <returns><see cref="Task"/></returns>
        public async Task Wait()
        {
                while (_isRunning)
                {
                        await Task.Delay(10, _cancellationToken.Token);
                }
        }

        /// <summary>
        /// Invokes the pipeline with a single request object then finialses the pipeline waits
        /// for it to complete and returns the output of the pipeline.
        /// </summary>
        /// <typeparam name="T">The type of the item to add to the context of the request.</typeparam>
        /// <param name="input">The object to include in the context of the request.</param>
        /// <returns><see cref="Context"/> of the result.</returns>
        public async Task<Context?> Invoke<T>(T input)
                where T : class
        {
                await Start();

                await AddInput<T>(input);

                await Finalise();

                var output = new List<Context>();
                await foreach (var item in Result.WithCancellation(_cancellationToken.Token))
                {
                        return item;
                }

                return null;
        }

        /// <summary>
        /// Invokes the pipeline with a multiple request objects then finialses the pipeline waits
        /// for it to complete and returns the output of the pipeline.
        /// </summary>
        /// <typeparam name="T">The type of the item to add to the context of the request.</typeparam>
        /// <param name="inputs">The object to include in the context of the request.</param>
        /// <param name="maxThreads">The maximum concurrecy to use when passing the inputs into the pipeline, defaults to the processor count <see cref="Environment.ProcessorCount" />.</param>
        /// <returns>Collection of <see cref="Context"/> of the result.</returns>
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

        /// <summary>
        /// Invokes the pipeline with a collection of inputs, each input is executed within the pipeline. 
        /// The pipeline is then finalised and the <see cref="IAsyncEnumerable{Context}"/> returns each 
        /// item as it is returned from the pipeline. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<Context> InvokeMany<T>(IEnumerable<T> inputs)
                where T : class
        {
                await Start();

                foreach (var input in inputs)
                {
                        await AddInput<T>(input);
                }

                await Finalise();

                await foreach (var item in Result.WithCancellation(_cancellationToken.Token))
                {
                        yield return item;
                }
        }

        /// <summary>
        /// Starts the pipeline allowing it 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="PipelineException"></exception>
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

        /// <summary>
        /// Disposes of the pipeline.
        /// </summary>
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