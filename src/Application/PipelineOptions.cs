internal class PipelineOptions
{
    public int MaxStepQueueSize {get;} = 10000;
    public int MaxStepConcurrency {get;} = 5;
}