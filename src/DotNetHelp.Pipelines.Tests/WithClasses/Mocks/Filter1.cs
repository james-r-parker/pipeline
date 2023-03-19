namespace DotNetHelp.Pipelines.Tests.WithClasses;

internal class Filter1 : PipelineFilterStep
{
        protected override Task<bool> Filter(PipelineRequest request)
        {
                if (request.Item.TryGetValue<SourceData>(out SourceData data))
                {
                        if(data.Id % 2 == 0)
                        {
                                return Task.FromResult(false);
                        }
                }

                return Task.FromResult(true);
        }
}
