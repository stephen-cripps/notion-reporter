namespace NotionReporter.Core.Models;

using System.Text.Json;

public record Member
{
    public string? Id { get; init; }
    public string? Name { get; init; }


    public Member(JsonElement jsonElement)
    {
        Id = jsonElement.GetProperty("id").GetString();
        Name = jsonElement.GetProperty("properties")
            .GetProperty("Member")
            .GetProperty("title")[0]
            .GetProperty("text")
            .GetProperty("content")
            .GetString();
    }
}
