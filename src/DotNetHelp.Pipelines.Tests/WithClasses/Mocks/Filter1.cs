namespace DotNetHelp.Pipelines.Tests.WithClasses;

internal class Filter1 : PipelineFilterStep
{
        protected override Task<bool> Filter(PipelineRequest request)
        {
                if (request.Item.TryGetValue<Step1Data>(out Step1Data data))
                {
                        if(data.Id % 2 == 0)
                        {
                                return Task.FromResult(true);
                        }
                }

                return Task.FromResult(false);
        }
}
