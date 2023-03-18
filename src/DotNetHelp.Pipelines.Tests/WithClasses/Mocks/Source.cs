namespace DotNetHelp.Pipelines.Tests.WithClasses;

internal class Source : PipelineSource
{
        private readonly Random _random;

        public Source(
            IOptionsMonitor<PipelineOptions> settings)
            : base(settings)
        {
                _random = new Random();
        }

        protected override async Task Process(PipelineRequest request)
        {
                await Task.Delay(_random.Next(5));

                if (request.Item.TryGetValue<SourceData>(out SourceData data))
                {
                        var json = new JsonObject
                        {
                                { "id", data.Id }
                        };
                        request.Item.Add(json);
                }
        }
}