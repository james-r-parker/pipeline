using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

internal class DataSource1 : PipelineSource
{
    private readonly DataSource1Client _client;

    public DataSource1(
        DataSource1.DataSource1Client client,
        IOptionsMonitor<PipelineOptions> settings)
        :base(settings)
    {
        _client = client;
    }

    protected override async Task Process(PipelineRequest request)
    {
        if(request.Item.TryGetValue<SourceData>(out SourceData data))
        {
            var candidate = await _client.GetData(data.CandidateId);
            request.Item.Add(candidate);
        }
    }

    public class DataSource1Client
    {
        private readonly HttpClient _httpClient;
        public DataSource1Client(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<JsonObject> GetData(int id)
        {
            using(var response = await _httpClient.GetAsync($"users"))
            {
                if(!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<JsonObject>(json);
            }
        }
    }
}