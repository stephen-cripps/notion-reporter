namespace NotionReporter.Core.Models;

using System.Text.Json.Serialization;

// It feels easier to just query everything and handle it in linq, rather than mess around with the notion query schema - which is why most of these are unused
// I should probably be more YAGNE but yolo - I might need to think about it a bit harder if performance demands it
public class QueryBody
{
    [JsonPropertyName("filter")]
    public string Filter { get; set; } = "";
    
    [JsonPropertyName("sorts")]
    public string Sorts { get; set; } = "";

    [JsonPropertyName("start_cursor")]
    public string StartCursor { get; set; } = "";

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }
}
