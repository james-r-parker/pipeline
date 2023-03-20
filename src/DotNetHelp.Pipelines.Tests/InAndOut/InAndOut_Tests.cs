namespace DotNetHelp.Pipelines.Tests.InlineTests.Sync;

public sealed class InAndOut_Tests : IDisposable
{
        private readonly Context _context;
        private readonly PipelineBuilder _builder;
        private readonly Pipeline _pipeline;
        private readonly CancellationToken _cancellationToken;

        public InAndOut_Tests()
        {
                _cancellationToken = new CancellationToken();
                _context = new Context();
                _builder = new PipelineBuilder(globalContext: _context);

                _builder
                        .AddInlineStep((r) =>
                        {
                                if (r.Item.TryGetValue(out SourceData data))
                                {
                                        data.Increment(data.Id);
                                }
                                return Task.CompletedTask;
                        })
                        .AddInlineFilterStep((r) =>
                        {
                                if (r.Item.TryGetValue(out SourceData data) && data.Id % 2 == 0)
                                {
                                        return Task.FromResult(false);
                                }
                                return Task.FromResult(true);
                        })
                        .AddInlineStep((r) =>
                        {
                                if (r.Item.TryGetValue(out SourceData data))
                                {
                                        data.Increment(data.Id);
                                }
                                return Task.CompletedTask;
                        });

                _pipeline = _builder.Build(_cancellationToken);
        }

        public async void Dispose()
        {
                _pipeline.Dispose();
        }

        [Fact]
        public async Task Single_Item()
        {
                await _pipeline.Start();

                var input = new SourceData(3);

                PipelineRequest item = await _pipeline.AddInput(input);
                Assert.True(item.Completed);

                Assert.Equal(6, item.Item.GetOrDefault<SourceData>()?.Updates);
        }
}