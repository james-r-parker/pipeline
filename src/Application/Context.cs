using System.Collections.Concurrent;

internal class Context
{
    public string Id {get; private set;}
    public DateTimeOffset Created {get;private set;}
    public DateTimeOffset Updated {get; private set;}
    private ConcurrentDictionary<Type, ConcurrentBag<object>> _context;
    private object _lock;

    public Context()
    {
        Id = Guid.NewGuid().ToString();
        Created = DateTimeOffset.UtcNow;
        _context = new ConcurrentDictionary<Type, ConcurrentBag<object>>();
        _lock = new Object();
    }

    public void Add<T>(T obj)
    {
        if(obj == null)
        {
            throw new ApplicationException("Not allowed to add null context");
        }

        lock(_lock)
        {
            if(_context.ContainsKey(obj.GetType()))
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
        if(_context.ContainsKey(typeof(T)))
        {
            return _context[typeof(T)].Select(x => (T)x).ToList();
        }

        return Enumerable.Empty<T>();
    }

    public bool TryGetValue<T>(out T value)
    {
        if(!_context.ContainsKey(typeof(T)))
        {
            value = default;
            return false;
        }

        value = _context[typeof(T)].Select(x => (T)x).LastOrDefault();
        return true;       
    }
}