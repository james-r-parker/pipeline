using Pipeline.Application;

namespace Pipeline;

public sealed class Pipeline : IDisposable
{
		private bool _isRunning;
		private bool _isFinalised;

		private readonly PipelineResult _result;
		private readonly Context _global;
		private readonly IEnumerable<IPipelineSource> _sources;
		private readonly IEnumerable<IPipelineStep> _steps;
		private readonly CancellationTokenSource _cancellationToken;

		internal Pipeline(
			Context globalContext,
			IEnumerable<IPipelineSource> sources,
			IEnumerable<IPipelineStep> steps,
			CancellationTokenSource cancellationToken)
		{
				_isRunning = false;
				_isFinalised = false;
				_sources = sources;
				_steps = steps;
				_global = globalContext;
				_cancellationToken = cancellationToken;
				_result = new PipelineResult(cancellationToken.Token);
		}

		public IAsyncEnumerable<Context> Result => _result;

		public async Task AddInput(Context input)
		{
				foreach (var source in _sources)
				{
						await source.Invoke(new PipelineRequest(_global, input));
				}
		}

		public async Task AddInput<T>(T input)
		{
				var request = new Context();
				request.Add<T>(input);
				await AddInput(request);
		}

		public Task AddOutput(Context output)
		{
				_result.Add(output);
				return Task.CompletedTask;
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

		public Task Invoke()
		{
				if (_isRunning)
				{
						throw new ApplicationException("Pipeline already running");
				}

				_isRunning = true;
				_result.Start();

				return Task.Run(async () =>
				{
						foreach (var step in _steps)
						{
								await step.Start();
						}

						foreach (var source in _sources)
						{
								await source.Start();
						}

						while (!_isFinalised)
						{
								await Task.Delay(10, _cancellationToken.Token);
						}

						while (_sources.Any(x => x.IsRunning))
						{
								await Task.Delay(10, _cancellationToken.Token);
						}

						while (_steps.Any(x => x.IsRunning))
						{
								await Task.Delay(10, _cancellationToken.Token);
						}

						_result.Stop();
						_isRunning = false;
				});
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
}