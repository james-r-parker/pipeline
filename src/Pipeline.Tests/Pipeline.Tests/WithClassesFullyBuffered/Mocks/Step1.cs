namespace Pipeline.Tests.WithClassesFullyBuffered;

internal class Step1 : PipelineBufferedStep
{
		public Step1(IOptionsMonitor<PipelineOptions> settings) : base(settings)
		{
		}

		protected override Task Process(PipelineRequest request)
		{
				request.Item.Add("hello");
				return Task.CompletedTask;
		}
}