using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed class MastMashupParams
{
    [JsonPropertyName("columns")]
    public string Columns { get; set; } = string.Empty;

    [JsonPropertyName("filters")]
    public List<MastMashupFilter> Filters { get; set; } = [];

    [JsonPropertyName("position")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Position { get; set; }

    [JsonPropertyName("radius")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Radius { get; set; }

    [JsonPropertyName("pagesize")]
    public int? PageSize { get; set; }
}
