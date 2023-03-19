using System.Diagnostics;

namespace DotNetHelp.Pipelines.Tests;

internal record SourceData
{
        public SourceData(int id)
        {
                Id = id;
        }

        public int Id { get; init; }
        public int Updates { get; private set; } = 0;

        [DebuggerStepThrough]
        public void Increment(int amount)
        {
                Updates += amount;
        }
}