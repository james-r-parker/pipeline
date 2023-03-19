namespace DotNetHelp.Pipelines.Tests.PipelineContext;

public class Context_Tests
{
        [Fact]
        public void Ids()
        {
                var context1 = new Context();
                var context2 = new Context();

                Assert.NotNull(context1.Id);
                Assert.NotEmpty(context1.Id);

                Assert.NotNull(context2.Id);
                Assert.NotEmpty(context2.Id);

                Assert.NotEqual(context1.Id, context2.Id);
        }

        [Fact]
        public void AddNull()
        {
                var context = new Context();
                Assert.Throws<PipelineContextException>(() => context.Add<ContextItem1>(null));
        }

        [Fact]
        public void GetOrDefault_Value()
        {
                var context = new Context();
                context.Add(new ContextItem1 { Id = 1 });
                context.Add(new ContextItem1 { Id = 2 });

                var item = context.GetOrDefault<ContextItem1>();
                Assert.NotNull(item);
                Assert.Equal(2, item.Id);
        }

        [Fact]
        public void GetOrDefault_Default()
        {
                var context = new Context();
                context.Add(new ContextItem1 { Id = 1 });
                context.Add(new ContextItem1 { Id = 2 });

                var item = context.GetOrDefault<ContextItem2>();
                Assert.Null(item);
        }

        [Fact]
        public void Get_Value()
        {
                var context = new Context();
                context.Add(new ContextItem1 { Id = 1 });
                context.Add(new ContextItem1 { Id = 2 });

                var items = context.Get<ContextItem1>();

                Assert.NotNull(items);
                Assert.Collection(
                        items,
                        x =>
                        {
                                Assert.Equal(1, x.Id);
                        },
                        x =>
                        {
                                Assert.Equal(2, x.Id);
                        });
        }

        [Fact]
        public void Get_Empty()
        {
                var context = new Context();
                context.Add(new ContextItem1 { Id = 1 });
                context.Add(new ContextItem1 { Id = 2 });

                var items = context.Get<ContextItem2>();

                Assert.Empty(items);
        }

        [Fact]
        public void TryGetValue_Value()
        {
                var context = new Context();
                context.Add(new ContextItem1 { Id = 1 });
                context.Add(new ContextItem1 { Id = 2 });

                var success = context.TryGetValue(out ContextItem1 val);

                Assert.True(success);
                Assert.Equal(2, val.Id);
        }

        [Fact]
        public void TryGetValue_Null()
        {
                var context = new Context();
                context.Add(new ContextItem1 { Id = 1 });
                context.Add(new ContextItem1 { Id = 2 });

                var success = context.TryGetValue(out ContextItem2 val);

                Assert.False(success);
                Assert.Null(val);
        }

        [Fact]
        public void Error()
        {
                var context = new Context();
                Assert.Empty(context.Errors);
                context.AddError("Step 1", new ApplicationException("I died"));

                Assert.Collection(
                        context.Errors,
                        x =>
                        {
                                Assert.Equal("Step 1", x.Key);
                                Assert.Collection(
                                        x.Value,
                                        y =>
                                        {
                                                Assert.IsType<ApplicationException>(y);
                                                Assert.Equal("I died", y.Message);
                                        });
                        });
        }

        internal record ContextItem1
        {
                public int Id { get; init; }
        }

        internal record ContextItem2
        {
                public int Id { get; init; }
        }
}
