namespace NotionReporter.Core.Models;

using System.Text.Json;

public record Event
{
    public string? Id { get; init; }
    
    public string? Name { get; init; }
    
    public DateTime Date { get; init; }
    
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
                .GetProperty("Due")
                .GetProperty("date")
                .GetProperty("start")
                .GetString() ?? string.Empty);
        }
        catch (Exception e)
        {
            // Sometimes the dates aren't there so we can ignore it. I think when I simplify notion and seperate the action tracker and the events this will go away
        }
    }
};
