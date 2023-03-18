namespace DotNetHelp.Pipelines.Tests.WithClasses;

internal class Source : PipelineSource
{
        public Source(
            IOptionsMonitor<PipelineOptions> settings)
            : base(settings)
        {
        }

        protected override Task Process(PipelineRequest request)
        {
                if (request.Item.TryGetValue<SourceData>(out SourceData data))
                {
                        var json = new JsonObject
                        {
                                { "id", data.Id }
                        };
                        request.Item.Add(json);
                }

                return Task.CompletedTask;
        }
}