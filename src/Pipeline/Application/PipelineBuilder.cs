namespace Pipeline;
public class PipelineBuilder
{
        private readonly IServiceCollection _services;

        public PipelineBuilder()
        {
                _services = new ServiceCollection();
                _services.Configure<PipelineOptions>((c) => { });
        }

        public PipelineBuilder ConfigureServices(Action<IServiceCollection> configure)
        {
                configure(_services);
                return this;
        }

        public PipelineBuilder AddSource<T>()
            where T : PipelineSource
        {
                _services
                    .AddSingleton<IPipelineSource, T>();

                return this;
        }

        public PipelineBuilder AddStep<T>()
            where T : PipelineStep
        {
                _services
                    .AddSingleton<IPipelineStep, T>();

                return this;
        }

        public PipelineBuilder AddInlineStep(Func<PipelineRequest, Task> inline)
        {
                _services
                    .AddSingleton<IPipelineStep>(c =>
                    {
                            return new PipelineInlineStep(inline);
                    });

                return this;
        }

        public PipelineBuilder AddInlineBufferedStep(Func<PipelineRequest, Task> inline)
        {
                _services
                    .AddSingleton<IPipelineStep>(c =>
                    {
                            var settings = c.GetRequiredService<IOptionsMonitor<PipelineOptions>>();
                            return new PipelineInlineBufferedStep(inline, settings);
                    });

                return this;
        }

        public Pipeline Build(CancellationToken cancellationToken = default, Context? globalContext = null)
        {
                CancellationTokenSource pipelineCancellationTokenSource = new CancellationTokenSource();

                if (cancellationToken != default)
                {
                        cancellationToken.Register(() =>
                        {
                                pipelineCancellationTokenSource.Cancel();
                        });
                }

                CancellationToken pipelineCancellationToken = pipelineCancellationTokenSource.Token;

                var provider = _services.BuildServiceProvider();

                var sources = provider.GetServices<IPipelineSource>().ToList();
                var steps = provider.GetServices<IPipelineStep>().ToList();

                var pipeline = new Pipeline(
                        globalContext ?? new Context(),
                        sources,
                        steps,
                        provider,
                        pipelineCancellationTokenSource);

                IPipelineStep previousStep = null;
                int index = 1;
                for (int i = steps.Count - 1; i >= 0; i--)
                {
                        steps[i].Name = $"Step {index++}. {steps[i].GetType().Name}";
                        steps[i].CancellationToken = pipelineCancellationToken;
                        if (previousStep != null)
                        {
                                steps[i].Next = previousStep.Invoke;
                        }
                        else
                        {
                                steps[i].Next = (r) => pipeline.AddOutput(r.Item);
                        }
                        previousStep = steps[i];
                }

                foreach (var source in sources)
                {
                        source.Name = source.GetType().Name;
                        source.Next = previousStep.Invoke;
                        source.CancellationToken = pipelineCancellationToken;
                }

                return pipeline;
        }
}