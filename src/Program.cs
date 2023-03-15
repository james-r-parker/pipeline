using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;

var cancellationToken = new CancellationToken();
var pipelineBuilder = new PipelineBuilder();

pipelineBuilder
    .ConfigureServices((services) => 
    {
        services
        .AddHttpClient<DataSource1.DataSource1Client>(c => 
        {
            c.BaseAddress = new Uri("https://random-data-api.com/api/v2/");
            c.Timeout = TimeSpan.FromSeconds(5);
        });
    })
    .AddSource<DataSource1>()
    .AddStep<Step1>()
    .AddStep<Step2>();

using(Pipeline pipe = pipelineBuilder.Build(cancellationToken))
{
    Context ctx = new Context();
    pipe.Invoke(ctx);

    foreach(int id in Enumerable.Range(1,5000))
    {
        await pipe.AddInput(new SourceData
        {
            CandidateId = id,
        });
    }

    pipe.Finalise();

    await foreach(var item in pipe)
    {
        if(item.TryGetValue<JsonObject>(out JsonObject obj))
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(obj));
        }
    }
}