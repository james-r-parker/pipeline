using Microsoft.Extensions.Options;

internal class Step1 : PipelineBufferedStep
{
    public Step1(IOptionsMonitor<PipelineOptions> settings) : base(settings)
    {
    }

    protected override Task Process(PipelineRequest request)
    {
        return Task.CompletedTask;
    }
}