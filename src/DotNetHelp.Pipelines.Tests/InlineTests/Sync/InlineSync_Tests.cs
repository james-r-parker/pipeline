namespace DotNetHelp.Pipelines.Tests.InlineTests.Sync;

public sealed class InlineSync_Tests : IDisposable
{
        private readonly Context _context;
        private readonly PipelineBuilder _builder;
        private readonly Pipeline _pipeline;
        private readonly CancellationToken _cancellationToken;

        public InlineSync_Tests()
        {
                _cancellationToken = new CancellationToken();
                _builder = new PipelineBuilder();

                _builder
                        .ConfigureServices(s =>
                        {
                                s.AddSingleton(new SourceData(100));
                        })
                        .AddInlineStep((r) =>
                        {
                                if (r.Item.TryGetValue(out SourceData data))
                                {
                                        data.Increment(data.Id);
                                }
                                r.Context.Add(new SourceData(1));
                                return Task.CompletedTask;
                        })
                        .AddInlineFilterStep((r) =>
                        {
                                if (r.Item.TryGetValue(out SourceData data) && data.Id != 3)
                                {
                                        return Task.FromResult(true);
                                }

                                return Task.FromResult(false);
                        })
                        .AddInlineStep((r) =>
                        {
                                throw new ApplicationException("I Died");
                        })
                        .AddInlineStep((r) =>
                        {
                                if (r.Item.TryGetValue(out SourceData data))
                                {
                                        data.Increment(data.Id);
                                }
                                r.Context.Add(new SourceData(2));
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
                var input = new SourceData(1);
                Context? result = await _pipeline.InvokeSync(input);
                Assert.NotNull(result);
                Assert.True(result.TryGetValue(out SourceData data));
                Assert.Equal(1, data.Id);
                Assert.Equal(2, data.Updates);
        }

        [Fact]
        public async Task Multiple_Item()
        {
                var input = new List<SourceData> { new SourceData(1), new SourceData(2) };
                IList<Context> result = await _pipeline.InvokeManySync(input);

                Assert.Collection(result.OrderBy(x => x.GetOrDefault<SourceData>()?.Id),
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

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        public async Task Multiple_Large(int count)
        {
                var input = Enumerable.Range(0, count).Select(x => new SourceData(x));
                IList<Context> result = await _pipeline.InvokeManySync(input);
                Assert.Equal(count - 1, result.Count);
        }

        [Fact]
        public async Task Filtered()
        {
                var input = new List<SourceData> { new SourceData(1), new SourceData(2), new SourceData(3), new SourceData(4) };
                IList<Context> result = await _pipeline.InvokeManySync(input);

                Assert.Collection(result.OrderBy(x => x.GetOrDefault<SourceData>()?.Id),
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
                        },
                        x =>
                        {
                                Assert.True(x.TryGetValue(out SourceData data));
                                Assert.Equal(4, data.Id);
                                Assert.Equal(8, data.Updates);
                        });
        }

        [Fact]
        public async Task On_Error()
        {
                var input = new SourceData(1);
                Context? result = await _pipeline.InvokeSync(input);
                Assert.NotNull(result);

                Assert.Collection(
                        result.Errors,
                        x =>
                        {
                                Assert.Equal("Step 2. PipelineInlineStep", x.Key);

                                Assert.Collection(
                                       x.Value,
                                       y =>
                                       {
                                               var ex = Assert.IsType<ApplicationException>(y);
                                               Assert.Equal("I Died", ex.Message);
                                       });
                        });

        }

        [Fact]
        public async Task Global_Context()
        {
                var input = new SourceData(1);
                await _pipeline.InvokeSync(input);
                var result = _pipeline.GlobalContext.Get<SourceData>();

                Assert.Collection(
                        result,
                        x =>
                        {
                                Assert.Equal(1, x.Id);
                        },
                        x =>
                        {
                                Assert.Equal(2, x.Id);
                        });
        }
}