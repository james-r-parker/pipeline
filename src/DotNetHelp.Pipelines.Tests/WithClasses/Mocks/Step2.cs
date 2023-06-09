namespace DotNetHelp.Pipelines.Tests.WithClasses;

internal class Step2 : PipelineBufferedStep
{
        private readonly Random _random;

        public Step2(IOptionsMonitor<PipelineOptions> settings) : base(settings)
        {
                _random = new Random();
        }

        protected override async Task Process(PipelineRequest request)
        {
                await Task.Delay(_random.Next(5));

                if (request.Item.TryGetValue<SourceData>(out SourceData data))
                {
                        request.Item.Add(new Step2Data { Id = data.Id });
                        data.Increment(data.Id);

                        if (data.Id == 1)
                        {
                                throw new ApplicationException("I Died");
                        }
                }

                request.Context.Add(new Step2Data { Id = 1 });
        }
}