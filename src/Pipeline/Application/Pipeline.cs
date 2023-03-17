using Pipeline.Application;

namespace Pipeline;

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
		private readonly bool _buffered;

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
				_buffered = steps.Any(x => x.GetType().IsAssignableTo(typeof(PipelineBufferedStep)));
				_result = new PipelineResult(cancellationToken.Token);
		}

		public IAsyncEnumerable<Context> Result => _result;

		public async Task<Context> AddInput(Context input)
		{
				var request = new PipelineRequest(_global, input, _serviceProvider.CreateScope().ServiceProvider);

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

				return input;
		}

		public Task<Context> AddInput<T>(T input)
		{
				var request = new Context();
				request.Add<T>(input);
				return AddInput(request);
		}

		public void Finalise()
		{
				_isFinalised = true;
		}

		public async Task Wait()
		{
				while (_isRunning)
				{
						await Task.Delay(10, _cancellationToken.Token);
				}
		}

		public async Task<Context> InvokeSync<T>(T input)
				where T : class
		{
				if (_buffered)
				{
						throw new PipelineException("You can not Invoke a pipeline with buffered steps synchronously");
				}

				await Invoke();

				var result = await AddInput<T>(input);

				Finalise();

				return result;
		}

		public async Task<IList<Context>> InvokeManySync<T>(IEnumerable<T> inputs, int? maxThreads = null)
		{
				if (_buffered)
				{
						throw new PipelineException("You can not Invoke a pipeline with buffered steps synchronously");
				}

				var task = await Invoke();

				var max = maxThreads.HasValue ? maxThreads.Value : Environment.ProcessorCount;
				var tasks = new List<Task<Context>>();

				using (var concurrency = new SemaphoreSlim(max, max))
				{
						foreach (var input in inputs)
						{
								await concurrency.WaitAsync();
								tasks.Add(Task.Run(async () =>
								{
										try
										{
												return await AddInput<T>(input);
										}
										finally
										{
												concurrency.Release();
										}
								}, _cancellationToken.Token));
						}

						await Task.WhenAll(tasks);
				}

				Finalise();

				return tasks.Select(x => x.Result).ToList();
		}

		public async IAsyncEnumerable<Context> InvokeMany<T>(IEnumerable<T> inputs)
		{
				var task = await Invoke();

				foreach (var input in inputs)
				{
						await AddInput<T>(input);
				}

				Finalise();

				await task;

				await foreach (var item in Result)
				{
						yield return item;
				}
		}

		public async Task<Task> Invoke()
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
				_cancellationToken.Cancel();

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