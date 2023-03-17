namespace Pipeline;

public class Context
{
		private readonly ConcurrentDictionary<Type, ConcurrentBag<object>> _context;
		private readonly ConcurrentDictionary<string, ConcurrentBag<Exception>> _errors;
		private readonly object _lock;

		public Context()
		{
				Id = Guid.NewGuid().ToString();
				Created = DateTimeOffset.UtcNow;
				Updated = Created;
				_context = new ConcurrentDictionary<Type, ConcurrentBag<object>>();
				_lock = new Object();
		}

		public string Id { get; init; }
		public DateTimeOffset Created { get; init; }
		public DateTimeOffset Updated { get; private set; }

		public void AddError(string stepName, Exception exception)
		{
				lock (_lock)
				{
						if (_errors.ContainsKey(stepName))
						{
								_errors[stepName].Add(exception);
						}
						else
						{
								_errors[stepName] = new ConcurrentBag<Exception> { exception };
						}

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
						if (_context.ContainsKey(obj.GetType()))
						{
								_context[obj.GetType()].Add(obj);
						}
						else
						{
								_context[obj.GetType()] = new ConcurrentBag<object> { obj };
						}

						Updated = DateTimeOffset.UtcNow;
				}
		}

		public IEnumerable<T> Get<T>()
		{
				if (_context.ContainsKey(typeof(T)))
				{
						return _context[typeof(T)].Select(x => (T)x).ToList();
				}

				return Enumerable.Empty<T>();
		}

		public bool TryGetValue<T>(out T value)
		{
				if (!_context.ContainsKey(typeof(T)))
				{
						value = default;
						return false;
				}

				value = _context[typeof(T)].Select(x => (T)x).LastOrDefault();
				return true;
		}
}