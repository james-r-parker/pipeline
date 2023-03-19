namespace DotNetHelp.Pipelines.Tests.WithClasses
{
        internal class Fork2 : PipelineForkStep
        {
                public Fork2(PipelineBuilder builder) : base(builder)
                {
                        builder
                                .AddStep<Step1>()
                                .AddStep<Step2>();
                }

                public override Task<bool> Filter(PipelineRequest request)
                {
                        if (request.Item.TryGetValue(out SourceData data) && data.Id % 11 == 0)
                        {
                                return Task.FromResult(true);
                        }
                        return Task.FromResult(false);
                }
        }
}
