namespace DotNetHelp.Pipelines.Tests.WithClasses;

internal class Step1 : PipelineStep
{
        protected override Task Process(PipelineRequest request)
        {
                if (request.Item.TryGetValue<SourceData>(out SourceData data))
                {
                        request.Item.Add(new Step1Data { Id = data.Id });
                        data.Increment(data.Id);
                }

                request.Context.Add(new Step1Data { Id = 1 });

                return Task.CompletedTask;
        }
}