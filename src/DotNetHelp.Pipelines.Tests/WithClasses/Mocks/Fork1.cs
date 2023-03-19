namespace DotNetHelp.Pipelines.Tests.WithClasses
{
        internal class Fork1 : PipelineForkStep
        {
                public Fork1(PipelineBuilder builder) : base(builder)
                {
                        builder
                                .AddStep<Step1>()
                                .AddFork(b => new Fork2(b))
                                .AddBranch(b => new Branch2(b))
                                .AddStep<Step2>();
                }

                public override Task<bool> Filter(PipelineRequest request)
                {
                        if (request.Item.TryGetValue(out SourceData data) && data.Id % 5 == 0)
                        {
                                return Task.FromResult(true);
                        }
                        return Task.FromResult(false);
                }
        }
}
