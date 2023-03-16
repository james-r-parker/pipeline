namespace Pipeline;

public sealed record PipelineRequest
{
		internal PipelineRequest(Context global)
		{
				Context = global;
				Item = new Context();
		}

		internal PipelineRequest(Context global, Context item)
		{
				Context = global;
				Item = item;
		}

		public Context Context { get; init; }

		public Context Item { get; init; }
}