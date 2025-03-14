namespace NotionReporter.Core.Services;

using System.Text;
using System.Text.Json;
using Models;

public class NotionService(string integrationSecret, string membersPageId)
{
    private const string BaseUrl = "https://api.notion.com/v1/databases/";

    public async Task<List<Member>> GetMembers()
    {
        var client = new HttpClient();

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {integrationSecret}");
        client.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

        var nextCursor = "0";

        var members = new List<Member>();

        do
        {
            var body = new QueryBody(){ StartCursor = nextCursor, };

            var bodyJson = JsonSerializer.Serialize(body);
            var httpContent = new StringContent(bodyJson, Encoding.UTF8, "text/plain");

            var response = await client.PostAsync($"{BaseUrl}{membersPageId}/query?filter_properties=title", httpContent);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Failed to get members: " + response.ReasonPhrase);

            var responseContent = await response.Content.ReadAsStringAsync();
            var rootElement = JsonDocument.Parse(responseContent).RootElement;

            members.AddRange(rootElement
                .GetProperty("results")
                .EnumerateArray()
                .Select(element => new Member(element)));

            nextCursor = rootElement.GetProperty("next_cursor").GetString();
        } while (!string.IsNullOrEmpty(nextCursor));

        return members;
    }
}
