using System.Diagnostics;

namespace DotNetHelp.Pipelines;

/// <summary>
/// Threadsafe context used within a pipeline.
/// </summary>
public class Context
{
        private readonly ConcurrentDictionary<Type, ConcurrentQueue<object>> _context;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<Exception>> _errors;
        private readonly object _lock;

        public Context()
        {
                Id = Guid.NewGuid().ToString();
                Created = DateTimeOffset.UtcNow;
                Updated = Created;
                _context = new ConcurrentDictionary<Type, ConcurrentQueue<object>>();
                _errors = new ConcurrentDictionary<string, ConcurrentQueue<Exception>>();
                _lock = new Object();
        }

        /// <summary>
        /// A unique ID for the context.
        /// </summary>
        public string Id { get; init; }

        /// <summary>
        /// Timestamp the context was created.
        /// </summary>
        public DateTimeOffset Created { get; init; }

        /// <summary>
        /// Timestamp the context was last written to.
        /// </summary>
        public DateTimeOffset Updated { get; private set; }

        /// <summary>
        /// Collection of errors that happened during the processing of the pipeline.
        /// </summary>
        public IImmutableDictionary<string, IImmutableList<Exception>> Errors => _errors.ToImmutableDictionary(x => x.Key, x => x.Value.ToImmutableList() as IImmutableList<Exception>);

        /// <summary>
        /// Add a new error to this context.
        /// </summary>
        /// <param name="stepName">The name of the step where the error happened.</param>
        /// <param name="exception">Exception details of the error that happened.</param>
        public void AddError(string stepName, Exception exception)
        {
                lock (_lock)
                {
                        if (!_errors.ContainsKey(stepName))
                        {
                                _errors[stepName] = new ConcurrentQueue<Exception>();
                        }

                        _errors[stepName].Enqueue(exception);
                        Updated = DateTimeOffset.UtcNow;
                }
        }

        /// <summary>
        /// Add a new object to the context.
        /// </summary>
        /// <typeparam name="T">Type of the object to add.</typeparam>
        /// <param name="obj">The object to add.</param>
        /// <exception cref="PipelineContextException">Thrown if the obj is null.</exception>
        [DebuggerStepThrough]
        public void Add<T>(T obj)
                where T : class
        {
                if (obj == null)
                {
                        throw new PipelineContextException("Not allowed to add null context");
                }

                lock (_lock)
                {
                        if (!_context.ContainsKey(obj.GetType()))
                        {
                                _context[obj.GetType()] = new ConcurrentQueue<object>();
                        }
                        _context[obj.GetType()].Enqueue(obj);
                        Updated = DateTimeOffset.UtcNow;
                }
        }

        /// <summary>
        /// Gets a collection of the items that have been added to the current context for a give type.
        /// </summary>
        /// <typeparam name="T">The type of the item(s) to get.</typeparam>
        /// <returns>Collection of items within this context.</returns>
        [DebuggerStepThrough]
        public IImmutableList<T> Get<T>()
                where T : class
        {
                if (_context.ContainsKey(typeof(T)))
                {
                        return _context[typeof(T)].Select(x => (T)x).ToImmutableList();
                }

                return ImmutableList<T>.Empty;
        }

        /// <summary>
        /// Trys to get the most recent item of a given type from the context.
        /// </summary>
        /// <typeparam name="T">Type of the item to find.</typeparam>
        /// <param name="value"></param>
        /// <returns>true if the item type was found otherwise false.</returns>
        [DebuggerStepThrough]
        public bool TryGetValue<T>(out T value)
                where T : class
        {
                if (!_context.ContainsKey(typeof(T)))
                {
                        value = default;
                        return false;
                }

                value = _context[typeof(T)].Last() as T;
                return true;
        }

        /// <summary>
        /// Gets the most recent item for a given type added to the context or the default of T.
        /// </summary>
        /// <typeparam name="T">The type of the item to get.</typeparam>
        /// <returns>T or null.</returns>
        [DebuggerStepThrough]
        public T? GetOrDefault<T>()
                where T : class
        {
                if (!_context.ContainsKey(typeof(T)))
                {
                        return default;
                }

                return _context[typeof(T)].Select(x => (T)x).LastOrDefault();
        }
}