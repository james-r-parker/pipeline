namespace Pipeline;

public class PipelineOptions
{
		public int MaxStepQueueSize { get; } = 10000;
		public int MaxStepConcurrency { get; } = 5;
		public int Wait = 5;
}