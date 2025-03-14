namespace NotionReporter.Core.Services;

using System.Text;
using System.Text.Json;
using Models;

public class NotionService
{
    private readonly string _membersPageId;
    private readonly string _eventsPageId;
    private readonly HttpClient _client;

    public NotionService(string integrationSecret, string membersPageId, string eventsPageId)
    {
        _membersPageId = membersPageId;
        _eventsPageId = eventsPageId;
        _client = new HttpClient();

        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {integrationSecret}");
        _client.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

    }

    private const string BaseUrl = "https://api.notion.com/v1/databases/";

    public async Task<List<Member>> GetMembers()
    {
        var nextCursor = "0";
        var members = new List<Member>();
        
        var events = await GetEvents();

        do
        {
            var body = new QueryBody(){ StartCursor = nextCursor, };

            var bodyJson = JsonSerializer.Serialize(body);
            var httpContent = new StringContent(bodyJson, Encoding.UTF8, "text/plain");

            var response = await _client.PostAsync($"{BaseUrl}{_membersPageId}/query", httpContent);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Failed to get members: " + response.ReasonPhrase);

            var responseContent = await response.Content.ReadAsStringAsync();
            var rootElement = JsonDocument.Parse(responseContent).RootElement;

            members.AddRange(rootElement
                .GetProperty("results")
                .EnumerateArray()
                .Select(element => new Member(element, events)));

            nextCursor = rootElement.GetProperty("next_cursor").GetString();
        } while (!string.IsNullOrEmpty(nextCursor));

        return members;
    }

    private async Task<List<Event>> GetEvents()
    {
        var nextCursor = "0";
        var events = new List<Event>();

        do
        {
            var body = new QueryBody(){ StartCursor = nextCursor, };

            var bodyJson = JsonSerializer.Serialize(body);
            var httpContent = new StringContent(bodyJson, Encoding.UTF8, "text/plain");

            var response = await _client.PostAsync($"{BaseUrl}{_eventsPageId}/query", httpContent);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Failed to get events: " + response.ReasonPhrase);

            var responseContent = await response.Content.ReadAsStringAsync();
            var rootElement = JsonDocument.Parse(responseContent).RootElement;

            events.AddRange(rootElement
                .GetProperty("results")
                .EnumerateArray()
                .Select(element => new Event(element)));

            nextCursor = rootElement.GetProperty("next_cursor").GetString();
        } while (!string.IsNullOrEmpty(nextCursor));

        return events;
    }
}
