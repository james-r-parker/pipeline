namespace DotNetHelp.Pipelines;

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

        public string Id { get; init; }
        public DateTimeOffset Created { get; init; }
        public DateTimeOffset Updated { get; private set; }
        public IImmutableDictionary<string, IImmutableList<Exception>> Errors => _errors.ToImmutableDictionary(x => x.Key, x => x.Value.ToImmutableList() as IImmutableList<Exception>);

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

        public void Add<T>(T obj)
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

        public IImmutableList<T> Get<T>()
        {
                if (_context.ContainsKey(typeof(T)))
                {
                        return _context[typeof(T)].Select(x => (T)x).ToImmutableList();
                }

                return ImmutableList<T>.Empty;
        }

        public bool TryGetValue<T>(out T value)
        {
                if (!_context.ContainsKey(typeof(T)))
                {
                        value = default;
                        return false;
                }

                value = _context[typeof(T)].Select(x => (T)x).Last();
                return true;
        }
}