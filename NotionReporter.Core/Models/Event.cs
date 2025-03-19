namespace NotionReporter.Core.Models;

using System.Text.Json;

public record Event
{
    public string? Id { get; init; }
    
    public string? Name { get; init; }
    
    public DateTime Date { get; init; }
    
    public List<string?> MembersAttended { get; init; }
    
    public List<string?> Tags { get; init; }
    
    public Event(JsonElement jsonElement)
    {
        Id = jsonElement.GetProperty("id").GetString();
        
        Name = jsonElement.GetProperty("properties")
            .GetProperty("Task name")
            .GetProperty("title")[0]
            .GetProperty("text")
            .GetProperty("content")
            .GetString();

        try
        {
            Date = DateTime.Parse(jsonElement.GetProperty("properties")
                .GetProperty("Date")
                .GetProperty("date")
                .GetProperty("start")
                .GetString() ?? string.Empty);
        }
        catch (Exception e)
        {
            // Sometimes the dates aren't there so we can ignore it. I think when I simplify notion and seperate the action tracker and the events this will go away
        }
        
        MembersAttended = jsonElement.GetProperty("properties")
            .GetProperty("Leaders")
            .GetProperty("relation")
            .EnumerateArray()
            .Select(element => element.GetProperty("id").GetString())
            .ToList();

        MembersAttended.AddRange(jsonElement.GetProperty("properties")
            .GetProperty("Participants")
            .GetProperty("relation")
            .EnumerateArray()
            .Select(element => element.GetProperty("id").GetString()));
        
        Tags = jsonElement.GetProperty("properties")
            .GetProperty("Tags")
            .GetProperty("multi_select")
            .EnumerateArray()
            .Select(element => element.GetProperty("name").GetString())
            .ToList();
    }
};
