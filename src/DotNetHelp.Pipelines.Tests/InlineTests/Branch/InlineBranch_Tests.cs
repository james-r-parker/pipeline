namespace DotNetHelp.Pipelines.Tests.InlineTests.Sync;

public sealed class InlineBranch_Tests : IDisposable
{
        private readonly Context _context;
        private readonly PipelineBuilder _builder;
        private readonly Pipeline _pipeline;
        private readonly CancellationToken _cancellationToken;

        public InlineBranch_Tests()
        {
                _cancellationToken = new CancellationToken();
                _builder = new PipelineBuilder();

                _builder
                        .ConfigureServices(s =>
                        {
                                s.AddSingleton(new SourceData(100));
                        })
                        .AddInlineBufferedStep((r) =>
                        {
                                if (r.Item.TryGetValue(out SourceData data))
                                {
                                        data.Increment(data.Id);
                                }
                                return Task.CompletedTask;
                        })
                        .AddInlineBranchStep((r) =>
                        {
                                if (r.Item.TryGetValue(out SourceData data) && data.Id % 2 == 0)
                                {
                                        return Task.FromResult(true);
                                }
                                return Task.FromResult(false);
                        },
                        () =>
                        {
                                var builder = new PipelineBuilder();
                                builder
                                        .AddInlineStep((r) =>
                                         {
                                                 if (r.Item.TryGetValue(out SourceData data))
                                                 {
                                                         data.Increment(data.Id);
                                                 }
                                                 return Task.CompletedTask;
                                         });
                                return builder;
                        })
                        .AddInlineStep((r) =>
                        {
                                if (r.Item.TryGetValue(out SourceData data))
                                {
                                        data.Increment(data.Id);
                                }
                                return Task.CompletedTask;
                        });

                _context = new Context();
                _pipeline = _builder.Build(_cancellationToken, globalContext: _context);
        }

        public void Dispose()
        {
                _pipeline.Dispose();
        }

        [Fact]
        public async Task Single_Item()
        {
                var input = new SourceData(2);
                Context? result = await _pipeline.Invoke(input);
                Assert.NotNull(result);
                Assert.True(result.TryGetValue(out SourceData data));
                Assert.Equal(2, data.Id);
                Assert.Equal(6, data.Updates);
        }

        [Fact]
        public async Task Multiple_Item()
        {
                var input = new List<SourceData> { new SourceData(1), new SourceData(2), new SourceData(3), new SourceData(4) };
                var result = await _pipeline.InvokeMany(input).OrderBy(x => x.GetOrDefault<SourceData>()?.Id).ToListAsync();

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
                                Assert.Equal(6, data.Updates);
                        },
                         x =>
                         {
                                 Assert.True(x.TryGetValue(out SourceData data));
                                 Assert.Equal(3, data.Id);
                                 Assert.Equal(6, data.Updates);
                         },
                        x =>
                        {
                                Assert.True(x.TryGetValue(out SourceData data));
                                Assert.Equal(4, data.Id);
                                Assert.Equal(12, data.Updates);
                        });
        }
}