namespace Pipeline;

public sealed record PipelineRequest
{
		internal PipelineRequest(Context global, IServiceProvider services)
		{
				Context = global;
				Item = new Context();
				Services = services;
		}

		internal PipelineRequest(Context global, Context item, IServiceProvider services)
		{
				Context = global;
				Item = item;
				Services = services;
		}

		public Context Context { get; init; }

		public Context Item { get; init; }

		public IServiceProvider Services { get; init; }
}