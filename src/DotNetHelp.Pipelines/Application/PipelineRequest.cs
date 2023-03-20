using System.Diagnostics;

namespace DotNetHelp.Pipelines;

/// <summary>
/// A single item that is to be processed by the pipeline.
/// </summary>
public sealed record PipelineRequest
{
        private readonly ConcurrentStack<Type> _steps;
        private bool _isComplete;

        [DebuggerStepThrough]
        internal PipelineRequest(Context global, Context item, IServiceProvider services)
        {
                Context = global;
                Item = item;
                Services = services;
                _steps = new ConcurrentStack<Type>();
                _isComplete = false;
                CreatedTimestamp = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Timestamp the request was created.
        /// </summary>
        public DateTimeOffset CreatedTimestamp { get; init; }

        /// <summary>
        /// Timestamp the request was marked as completed.
        /// </summary>
        public DateTimeOffset? CompletedTimestamp { get; private set; }

        /// <summary>
        /// The global context that is shared across all requests within the pipeline.
        /// </summary>
        public Context Context { get; init; }

        /// <summary>
        /// The context of the current item that the pipeline is being executed for.
        /// </summary>
        public Context Item { get; init; }

        /// <summary>
        /// A scoped service provider for this single request item.
        /// </summary>
        public IServiceProvider Services { get; init; }

        /// <summary>
        /// A collection of the steps the request has gone through.
        /// </summary>
        public IReadOnlyCollection<Type> Steps => _steps;

        /// <summary>
        /// True if the request has run through pipeline until the end.
        /// </summary>
        public bool Completed => _isComplete;

        /// <summary>
        /// Adds a step that this request passed through to the collection of steps this request has passed through.
        /// </summary>
        /// <param name="step">The step this request has just passed through.</param>
        internal void AddStep(PipelineStep step)
        {
                _steps.Push(step.GetType());
        }

        /// <summary>
        /// Mark the request as complete.
        /// </summary>
        internal void Complete()
        {
                if (!_isComplete)
                {
                        _isComplete = true;
                        CompletedTimestamp = DateTimeOffset.UtcNow;
                }
        }
}