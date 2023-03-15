internal sealed record PipelineRequest
{
    public PipelineRequest(Context global)
    {
        Context = global;
        Item = new Context();
    }

    public PipelineRequest(Context global, Context item)
    {
        Context = global;
        Item = item;
    }

    public Context Context {get; init;}

    public Context Item {get; init;}
}