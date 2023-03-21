namespace DotNetHelp.Pipelines.Tests.SafetyChecks
{
        public class SafetyChecks_Tests : IDisposable
        {
                private readonly Context _context;
                private readonly PipelineBuilder _builder;
                private readonly Pipeline _pipeline;
                private readonly CancellationToken _cancellationToken;

                public SafetyChecks_Tests()
                {
                        _cancellationToken = new CancellationToken();
                        _context = new Context();
                        _builder = new PipelineBuilder(globalContext: _context);

                        _builder
                                .AddInlineStep((r) =>
                                {
                                        return Task.CompletedTask;
                                });

                        _pipeline = _builder.Build(_cancellationToken);
                }

                public void Dispose()
                {
                        _pipeline.Dispose();
                }

                [Fact]
                public async Task Start_Multiple()
                {
                        await _pipeline.Start();
                        await Assert.ThrowsAsync<PipelineException>(async () => await _pipeline.Start());
                }

                [Fact]
                public async Task Finalise_Without_Start()
                {
                        await Assert.ThrowsAsync<PipelineException>(async () => await _pipeline.Finalise());
                }

                [Fact]
                public async Task Finalise_Multiple()
                {
                        await _pipeline.Start();
                        await _pipeline.Finalise();
                        await Assert.ThrowsAsync<PipelineException>(async () => await _pipeline.Finalise());
                }

                [Fact]
                public async Task AddInput_Finalised()
                {
                        await _pipeline.Start();
                        await _pipeline.Finalise();
                        await Assert.ThrowsAsync<PipelineException>(async () => await _pipeline.AddInput(new object()));
                }
        }
}
