using System.Diagnostics;

namespace DotNetHelp.Pipelines.Tests;

internal record SourceData
{
        [DebuggerStepThrough]
        public SourceData(int id)
        {
                Id = id;
                Updates = 0;
        }

        public int Id { get; init; }

        public int Updates { get; private set; }

        [DebuggerStepThrough]
        public void Increment(int amount)
        {
                Updates += amount;
        }
}