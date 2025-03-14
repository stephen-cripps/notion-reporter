namespace NotionReporter.Core.Models;

using System.Text.Json;

public record Member
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public List<Event?> EventsAttended { get; init; }

    public Member(JsonElement jsonElement, IEnumerable<Event> events)
    {
        Id = jsonElement.GetProperty("id").GetString();
        
        Name = jsonElement.GetProperty("properties")
            .GetProperty("Member")
            .GetProperty("title")[0]
            .GetProperty("text")
            .GetProperty("content")
            .GetString();
        
        EventsAttended = jsonElement.GetProperty("properties")
            .GetProperty("Actions Attended")
            .GetProperty("relation")
            .EnumerateArray()
            .Select(element => element.GetProperty("id").GetString())
            .Select(x => events.SingleOrDefault(y => y.Id == x))
            .ToList();
        
        EventsAttended.AddRange(
            jsonElement.GetProperty("properties")
                .GetProperty("Actions led")
                .GetProperty("relation")
                .EnumerateArray()
                .Select(element => element.GetProperty("id").GetString())
                .Select(x => events.SingleOrDefault(y => y.Id == x))
                .ToList());
    }
}
