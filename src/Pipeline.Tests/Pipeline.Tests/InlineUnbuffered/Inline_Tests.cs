using Pipeline.Tests.WithClassesFullyBuffered;

namespace Pipeline.Tests.InlineUnbuffered;

public class Inline_Tests
{
		[Fact]
		public async Task Run()
		{
				var cancellationToken = new CancellationToken();
				var pipelineBuilder = new PipelineBuilder();

				pipelineBuilder
					.AddInlineStep((r) =>
					{
							return Task.CompletedTask;
					})
					.AddInlineStep((r) =>
					{
							return Task.CompletedTask;
					});

				Context ctx = new Context();

				using (Pipeline pipe = pipelineBuilder.Build(cancellationToken, ctx))
				{
						pipe.Invoke();

						foreach (int id in Enumerable.Range(1, 10))
						{
								await pipe.AddInput(new SourceData
								{
										Id = id,
								});
						}

						pipe.Finalise();

						var output = new List<Context>();

						await foreach (var item in pipe.Result)
						{
								output.Add(item);
						}

						Assert.Equal(10, output.Count);
				}
		}
}