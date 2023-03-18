namespace DotNetHelp.Pipelines.Tests.WithClasses;

public class WithClasses_Tests : IDisposable
{
        private readonly Context _context;
        private readonly PipelineBuilder _builder;
        private readonly Pipeline _pipeline;
        private readonly CancellationToken _cancellationToken;

        public WithClasses_Tests()
        {
                _cancellationToken = new CancellationToken();
                _builder = new PipelineBuilder();

                _builder
                    .AddSource<Source>()
                    .AddStep<Step1>()
                    .AddFilter<Filter1>()
                    .AddStep<Step2>();

                _context = new Context();

                _pipeline = _builder.Build(_cancellationToken, globalContext: _context);
        }

        public void Dispose()
        {
                _pipeline.Dispose();
        }

        [Fact]
        public async Task Invoke()
        {
                await _pipeline.Invoke();

                foreach (int id in Enumerable.Range(1, 1000))
                {
                        await _pipeline.AddInput(new SourceData(id));
                }

                _pipeline.Finalise();

                var output = new List<Context>();

                await foreach (var item in _pipeline.Result)
                {
                        output.Add(item);
                }

                Assert.Equal(500, output.Count);
        }
}