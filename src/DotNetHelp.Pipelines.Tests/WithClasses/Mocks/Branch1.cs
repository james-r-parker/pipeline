namespace DotNetHelp.Pipelines.Tests.WithClasses
{
        internal class Branch1 : PipelineBranchStep
        {
                public Branch1(PipelineBuilder builder) : base(builder)
                {
                        builder
                                .AddStep<Step1>()
                                .AddFork(b => new Fork2(b))
                                .AddBranch(b => new Branch2(b))
                                .AddStep<Step2>();
                }

                public override Task<bool> Filter(PipelineRequest request)
                {
                        if (request.Item.TryGetValue(out SourceData data) && data.Id % 3 == 0)
                        {
                                return Task.FromResult(true);
                        }
                        return Task.FromResult(false);
                }
        }
}
