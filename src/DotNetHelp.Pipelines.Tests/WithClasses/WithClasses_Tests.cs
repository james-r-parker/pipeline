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
                _context = new Context();
                _builder = new PipelineBuilder(globalContext: _context);

                _builder
                    .AddSource<Source>()
                    .AddFilter<Filter1>()
                    .AddStep<Step1>()
                    .AddBranch(builder => new Branch1(builder))
                    .AddFork(builder => new Fork1(builder))
                    .AddStep<Step2>();

                _pipeline = _builder.Build(_cancellationToken);
        }

        public void Dispose()
        {
                _pipeline.Dispose();
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(3, 12)]
        [InlineData(21, 126)]
        [InlineData(33, 132)]
        [InlineData(55, 220)]
        [InlineData(63, 378)]
        public async Task Single_Item(int start, int output)
        {
                var input = new SourceData(start);
                Context? result = await _pipeline.Invoke(input);
                Assert.NotNull(result);
                Assert.True(result.TryGetValue(out SourceData data));
                Assert.Equal(start, data.Id);
                Assert.Equal(output, data.Updates);
        }

        [Fact]
        public async Task Multiple_Item()
        {
                var input = new List<SourceData>
                {
                        new SourceData(1),
                        new SourceData(2),
                        new SourceData(3),
                        new SourceData(4),
                        new SourceData(5),
                        new SourceData(6),
                        new SourceData(7),
                        new SourceData(8),
                        new SourceData(9),
                        new SourceData(10),
                        new SourceData(11),
                        new SourceData(12)
                };

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
                                Assert.Equal(3, data.Id);
                                Assert.Equal(12, data.Updates);
                        },
                        x =>
                        {
                                Assert.True(x.TryGetValue(out SourceData data));
                                Assert.Equal(5, data.Id);
                                Assert.Equal(15, data.Updates);
                        },
                        x =>
                        {
                                Assert.True(x.TryGetValue(out SourceData data));
                                Assert.Equal(7, data.Id);
                                Assert.Equal(14, data.Updates);
                        },
                        x =>
                        {
                                Assert.True(x.TryGetValue(out SourceData data));
                                Assert.Equal(9, data.Id);
                                Assert.Equal(36, data.Updates);
                        },
                        x =>
                        {
                                Assert.True(x.TryGetValue(out SourceData data));
                                Assert.Equal(11, data.Id);
                                Assert.Equal(22, data.Updates);
                        });
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(2000)]
        public async Task Multiple_Large(int count)
        {
                var input = Enumerable.Range(0, count).Select(x => new SourceData(x));
                List<Context> result = await _pipeline.InvokeMany(input).OrderBy(x => x.GetOrDefault<SourceData>()?.Id).ToListAsync();
                var ids = result.Select(x => x.GetOrDefault<SourceData>()?.Id).ToList();

                foreach (int? num in Enumerable.Range(0, count))
                {
                        if (num % 2 != 0)
                        {
                                Assert.Contains(num, ids);
                        }
                }

                Assert.Equal(count / 2, result.Count);
        }

        [Fact]
        public async Task Filtered()
        {
                var input = new List<SourceData> { new SourceData(1), new SourceData(2), new SourceData(3), new SourceData(4) };
                var result = await _pipeline.InvokeMany(input).OrderBy(x => x.GetOrDefault<SourceData>()?.Id).ToListAsync();

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
                                Assert.Equal(3, data.Id);
                                Assert.Equal(12, data.Updates);
                        });
        }

        [Fact]
        public async Task On_Error()
        {
                var input = new SourceData(1);
                Context? result = await _pipeline.Invoke(input);
                Assert.NotNull(result);

                Assert.Collection(
                        result.Errors,
                        x =>
                        {
                                Assert.Equal("Step 1. Step2", x.Key);

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
                var input = new List<SourceData> { new SourceData(2), new SourceData(3), new SourceData(7), new SourceData(21), new SourceData(22), new SourceData(55) };
                await _pipeline.InvokeMany(input).OrderBy(x => x.GetOrDefault<SourceData>()?.Id).ToListAsync();

                var result1 = _pipeline.GlobalContext.Get<Step1Data>();
                Assert.Equal(9, result1.Count);

                var result2 = _pipeline.GlobalContext.Get<Step2Data>();
                Assert.Equal(9, result1.Count);
        }
}