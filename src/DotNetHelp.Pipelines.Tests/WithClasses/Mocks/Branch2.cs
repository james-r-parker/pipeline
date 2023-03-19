namespace DotNetHelp.Pipelines.Tests.WithClasses
{
        internal class Branch2 : PipelineBranchStep
        {
                public Branch2(PipelineBuilder builder) : base(builder)
                {
                        builder
                                .AddStep<Step1>()
                                .AddStep<Step2>();
                }

                public override Task<bool> Filter(PipelineRequest request)
                {
                        if (request.Item.TryGetValue(out SourceData data) && data.Id % 7 == 0)
                        {
                                return Task.FromResult(true);
                        }
                        return Task.FromResult(false);
                }
        }
}
