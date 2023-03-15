using System.Collections.Concurrent;

internal sealed class Pipeline : IAsyncEnumerator<Context>, IAsyncEnumerable<Context>, IDisposable
{
    private bool _isRunning;
    private bool _isFinalised;
    private Context _global;
    private readonly ConcurrentQueue<Context> _output;
    private readonly IEnumerable<IPipelineSource> _sources;
    private readonly IEnumerable<IPipelineStep> _steps;
    private readonly CancellationTokenSource _cancellationToken;

    public Context Current {get;set;}

    public Pipeline(
        IEnumerable<IPipelineSource> sources,
        IEnumerable<IPipelineStep> steps,
        CancellationTokenSource cancellationToken)
    {
        _output = new ConcurrentQueue<Context>();
        _isRunning = false;
        _isFinalised = false;
        _sources = sources;
        _steps = steps;
        _cancellationToken = cancellationToken;
    }

    public async ValueTask<bool> MoveNextAsync()
    {
        if(!_isRunning && _output.Count == 0)
        {
            return false;
        }

        if(_output.Count > 0 && _output.TryDequeue(out Context i))
        {
            Current = i;
            return true;
        }

        while(_isRunning && _output.Count == 0)
        {
            await Task.Delay(100, _cancellationToken.Token);
        }

        if(!_isRunning && _output.Count == 0)
        {
            return false;
        }

        if(_output.Count > 0 && _output.TryDequeue(out Context ii))
        {
            Current = ii;
            return true;
        }

        return false;
    }

    public ValueTask DisposeAsync()
    {
        _output.Clear();
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        _cancellationToken.Cancel();

        foreach(var source in _sources)
        {
            source.Dispose();
        }

        foreach(var step in _steps)
        {
            step.Dispose();            
        }

        _output.Clear();
    }

    public async IAsyncEnumerator<Context> GetAsyncEnumerator(CancellationToken cancellationToken)
    {
        while(await MoveNextAsync())
        {
            yield return Current;
        }
    }

    public async Task AddInput(Context input)
    {
        foreach(var source in _sources)
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
        _output.Enqueue(output);
        return Task.CompletedTask;
    }

    public void Finalise()
    {
        _isFinalised = true;
    }

    public async Task Wait()
    {
        while(_isRunning)
        {
            await Task.Delay(500);
        }
    }

    public async Task Invoke(Context context)
    {
        if(_isRunning)
        {
            throw new ApplicationException("Pipeline already running");
        }

        _global = context;
        _isRunning = true;
        foreach (var step in _steps)
        {
            await step.Start();
        }

        foreach (var source in _sources)
        {
            await source.Start();
        }

        while(!_isFinalised)
        {
            await Task.Delay(100);
        }

        while (_sources.Any(x => x.IsRunning))
        {
            await Task.Delay(100);
        }

        while (_steps.Any(x => x.IsRunning))
        {
            await Task.Delay(100);
        }

        _isRunning = false;
    }
}