namespace Pipeline.Tests.Inline;

public sealed class InlineTests_Tests : IDisposable
{
        private readonly Context _context;
        private readonly PipelineBuilder _builder;
        private readonly Pipeline _pipeline;
        private readonly CancellationToken _cancellationToken;

        public InlineTests_Tests()
        {
                _cancellationToken = new CancellationToken();
                _builder = new PipelineBuilder();

                _builder
                    .AddInlineStep((r) =>
                    {
                            if (r.Item.TryGetValue(out SourceData data))
                            {
                                    data.Increment();
                            }
                            r.Context.Add(new SourceData(1));
                            return Task.CompletedTask;
                    })
                    .AddInlineStep((r) =>
                    {
                            throw new ApplicationException("I Died");
                    })
                    .AddInlineStep((r) =>
                    {
                            if (r.Item.TryGetValue(out SourceData data))
                            {
                                    data.Increment();
                            }
                            r.Context.Add(new SourceData(2));
                            return Task.CompletedTask;
                    });

                _context = new Context();
                _pipeline = _builder.Build(_cancellationToken, _context);
        }

        public void Dispose()
        {
                _pipeline.Dispose();
        }

        [Fact]
        public async Task No_Source_Unbuffered_Single_Item_Sync()
        {
                var input = new SourceData(1);
                Context result = await _pipeline.InvokeSync(input);
                Assert.True(result.TryGetValue(out SourceData data));
                Assert.Equal(1, data.Id);
                Assert.Equal(2, data.Updates);
        }

        [Fact]
        public async Task No_Source_Unbuffered_Multiple_Item_Sync()
        {
                var input = new List<SourceData> { new SourceData(1), new SourceData(2) };
                IList<Context> result = await _pipeline.InvokeManySync(input);

                Assert.Collection(result,
                        x =>
                        {
                                Assert.True(x.TryGetValue(out SourceData data));
                                Assert.Equal(1, data.Id);
                                Assert.Equal(2, data.Updates);
                        },
                        x =>
                        {
                                Assert.True(x.TryGetValue(out SourceData data));
                                Assert.Equal(2, data.Id);
                                Assert.Equal(4, data.Updates);
                        });
        }

        [Fact]
        public async Task On_Error()
        {
                var input = new SourceData(1);
                Context result = await _pipeline.InvokeSync(input);

                Assert.Collection(result.)

        }
}