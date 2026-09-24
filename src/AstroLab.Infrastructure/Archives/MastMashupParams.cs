using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record MastMashupParams
{
    private MastMashupParams(string columns, List<MastMashupFilter> filters, string? position)
    {
        Columns = columns;
        Filters = filters;
        Position = position;
    }

    [JsonPropertyName("columns")]
    public string Columns { get; }

    [JsonPropertyName("filters")]
    public List<MastMashupFilter> Filters { get; }

    [JsonPropertyName("position")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Position { get; }

    public static MastMashupParams Create(string columns, List<MastMashupFilter> filters, string? position = null) =>
        new(columns, filters, position);
}
