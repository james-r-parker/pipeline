namespace DotNetHelp.Pipelines;
public class PipelineBuilder
{
        private readonly IServiceCollection _services;
        private readonly PipelineBuilder? _parent;
        private readonly Context? _globalContext;
        private Pipeline? _pipeline;

        /// <summary>
        /// Creates a new pipeline builder.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="globalContext"></param>
        public PipelineBuilder(PipelineBuilder? parent = null, Context? globalContext = null)
        {
                _services = new ServiceCollection();
                _services.Configure<PipelineOptions>((c) => { });
                _parent = parent;
                _globalContext = globalContext;
        }

        /// <summary>
        /// Configures a collection of services that are used to produce the 
        /// pipeline and that are available via the context during the pipeline execution.
        /// </summary>
        /// <param name="configure">Action to configure the services.</param>
        /// <returns>this to allow chaining</returns>
        public PipelineBuilder ConfigureServices(Action<IServiceCollection> configure)
        {
                configure(_services);
                return this;
        }

        /// <summary>
        /// Adds a source to the pipeline. A source is step for whoms output will 
        /// trigger all pipeline steps to be executed. For example  a pipeline with a 
        /// single source will have all steps triggered once per input to the pipeline, 
        /// but a pipeline with 3 sources will have all steps triggered 3 times per input 
        /// into the pipeline.
        /// </summary>
        /// <typeparam name="T">The type of class to register.</typeparam>
        /// <returns>this to allow chaining</returns>
        public PipelineBuilder AddSource<T>()
            where T : PipelineSource
        {
                _services
                    .AddTransient<IPipelineSource, T>();

                return this;
        }

        /// <summary>
        /// Adds a step to the pipeline. A step will be triggered for each input into the pipeline
        /// and has access to context passed through all previous steps.
        /// </summary>
        /// <typeparam name="T">The type of class to register.</typeparam>
        /// <returns>this to allow chaining</returns>
        public PipelineBuilder AddStep<T>()
            where T : PipelineStep
        {
                _services
                    .AddTransient<IPipelineStep, T>();

                return this;
        }

        /// <summary>
        /// Adds a filter to the pipeline. A filter will allow the request to continue to
        /// following steps if the filter returns true. If the filter returns false the context
        /// will be discarded and no futher steps will be executed for this input.
        /// </summary>
        /// <typeparam name="T">The type of class to register.</typeparam>
        /// <returns>this to allow chaining</returns>
        public PipelineBuilder AddFilter<T>()
                where T : PipelineFilterStep
        {
                _services
                    .AddTransient<IPipelineStep, T>();

                return this;
        }

        /// <summary>
        /// Adds a branch to the pipeline. A branch is a sub pipeline that once completed
        /// will continue back to parent pipeline. The branch will only be executed if the 
        /// filter returns true. If the filter returns false the pipeline will continue to the 
        /// next steps without executing the branch sub pipeline.
        /// </summary>
        /// <typeparam name="T">The type of class to register.</typeparam>
        /// <param name="ctor"></param>
        /// <returns>this to allow chaining</returns>
        public PipelineBuilder AddBranch<T>(Func<PipelineBuilder, T> ctor)
                where T : PipelineBranchStep
        {
                _services
                    .AddTransient<IPipelineStep, T>(c => ctor(new PipelineBuilder(this, _globalContext)));

                return this;
        }

        /// <summary>
        /// Adds a fork to the pipeline. A fork is a sub pipeline that once completed
        /// will return its output. If a fork is triggered no further pipeline steps will be 
        /// executed for this request. The fork will only be executed if the 
        /// filter returns true. If the filter returns false the pipeline will continue to the 
        /// next steps without executing the fork sub pipeline.
        /// </summary>
        /// <typeparam name="T">The type of class to register.</typeparam>
        /// <param name="ctor"></param>
        /// <returns>this to allow chaining</returns>
        public PipelineBuilder AddFork<T>(Func<PipelineBuilder, T> ctor)
                where T : PipelineForkStep
        {
                _services
                    .AddTransient<IPipelineStep, T>(c => ctor(new PipelineBuilder(this, _globalContext)));

                return this;
        }

        /// <summary>
        /// <see cref="PipelineBuilder.AddFilter{T}" /> but allows the step to be added inline without the need to create a class.
        /// </summary>
        /// <param name="inline"></param>
        /// <returns>this to allow chaining</returns>
        public PipelineBuilder AddInlineFilterStep(Func<PipelineRequest, Task<bool>> inline)
        {
                _services
                    .AddTransient<IPipelineStep>(c =>
                    {
                            return new PipelineInlineFilterStep(inline);
                    });

                return this;
        }

        /// <summary>
        /// <see cref="PipelineBuilder.AddStep{T}" /> but allows the step to be added inline without the need to create a class.
        /// </summary>
        /// <param name="inline"></param>
        /// <returns>this to allow chaining</returns>
        public PipelineBuilder AddInlineStep(Func<PipelineRequest, Task> inline)
        {
                _services
                    .AddTransient<IPipelineStep>(c =>
                    {
                            return new PipelineInlineStep(inline);
                    });

                return this;
        }

        /// <summary>
        /// <see cref="PipelineBuilder.AddStep{T}" /> but allows the step to be added inline without the need to create a class.
        /// </summary>
        /// <param name="inline"></param>
        /// <returns>this to allow chaining</returns>
        public PipelineBuilder AddInlineBufferedStep(Func<PipelineRequest, Task> inline)
        {
                _services
                    .AddTransient<IPipelineStep>(c =>
                    {
                            var settings = c.GetRequiredService<IOptionsMonitor<PipelineOptions>>();
                            return new PipelineInlineBufferedStep(inline, settings);
                    });

                return this;
        }

        /// <summary>
        /// <see cref="PipelineBuilder.AddBranch{T}" /> but allows the step to be added inline without the need to create a class.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="configure"></param>
        /// <returns>this to allow chaining</returns>
        public PipelineBuilder AddInlineBranchStep(Func<PipelineRequest, Task<bool>> filter, Action<PipelineBuilder> configure)
        {
                _services
                    .AddTransient<IPipelineStep>(c =>
                    {
                            var builder = new PipelineBuilder(this, _globalContext);
                            configure(builder);
                            return new PipelineInlineBranchStep(builder, filter);
                    });

                return this;
        }

        /// <summary>
        /// <see cref="PipelineBuilder.AddFork{T}" /> but allows the step to be added inline without the need to create a class.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="configure"></param>
        /// <returns>this to allow chaining</returns>
        public PipelineBuilder AddInlineForkStep(Func<PipelineRequest, Task<bool>> filter, Action<PipelineBuilder> configure)
        {
                _services
                    .AddTransient<IPipelineStep>(c =>
                    {
                            var builder = new PipelineBuilder(this, _globalContext);
                            configure(builder);
                            return new PipelineInlineForkStep(builder, filter);
                    });

                return this;
        }

        /// <summary>
        /// Create a pipeline that can be executed.
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token used to cancel the pipeline if required.</param>
        /// <param name="output">Function used to override where the output of the pipeline is delivered to.</param>
        /// <returns>A pipeline.</returns>
        public Pipeline Build(
                CancellationToken cancellationToken = default,
                Func<PipelineRequest, Task>? output = null)
        {
                if (_pipeline != null)
                {
                        return _pipeline;
                }

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
                using (var scope = provider.CreateScope())
                {
                        var sources = provider.GetServices<IPipelineSource>().ToList();
                        var steps = provider.GetServices<IPipelineStep>().ToList();

                        _pipeline = new Pipeline(
                                _globalContext ?? new Context(),
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

                                if (steps[i] is PipelineForkStep fork)
                                {
                                        fork.End = (r) =>
                                        {
                                                if (_parent != null)
                                                {
                                                        return ChainOutput(r.Item);
                                                }
                                                else
                                                {
                                                        return _pipeline.AddOutput(r.Item);
                                                }
                                        };
                                }

                                if (previousStep != null)
                                {
                                        steps[i].Next = previousStep.Invoke;
                                }
                                else
                                {
                                        steps[i].Next = (r) =>
                                        {
                                                if (output != null)
                                                {
                                                        return output(r);
                                                }
                                                else
                                                {
                                                        if (_parent != null)
                                                        {
                                                                return ChainOutput(r.Item);
                                                        }
                                                        else
                                                        {
                                                                return _pipeline.AddOutput(r.Item);
                                                        }
                                                }
                                        };
                                }
                                previousStep = steps[i];
                        }

                        foreach (var source in sources)
                        {
                                source.Name = source.GetType().Name;
                                source.Next = previousStep.Invoke;
                                source.CancellationToken = pipelineCancellationToken;
                        }

                        return _pipeline;
                }
        }

        internal Task ChainOutput(Context context)
        {
                if (_parent != null)
                {
                        return _parent.ChainOutput(context);
                }

                if (_pipeline != null)
                {
                        return _pipeline.AddOutput(context);
                }

                throw new PipelineException("Misconfigured Pipeline");
        }
}