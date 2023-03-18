namespace DotNetHelp.Pipelines.Tests.WithClasses;

internal class Step2 : PipelineBufferedStep
{
        public Step2(IOptionsMonitor<PipelineOptions> settings) : base(settings)
        {
        }

        protected override Task Process(PipelineRequest request)
        {
                request.Item.Add(1);
                return Task.CompletedTask;
        }
}