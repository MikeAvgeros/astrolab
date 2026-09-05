using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record MastMashupParams
{
    [JsonConstructor]
    internal MastMashupParams(
        string columns,
        List<MastMashupFilter> filters,
        string? position,
        double? radius,
        int? pageSize)
    {
        Columns = columns;
        Filters = filters;
        Position = position;
        Radius = radius;
        PageSize = pageSize;
    }

    [JsonPropertyName("columns")]
    public string Columns { get; }

    [JsonPropertyName("filters")]
    public List<MastMashupFilter> Filters { get; }

    [JsonPropertyName("position")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Position { get; }

    [JsonPropertyName("radius")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Radius { get; }

    [JsonPropertyName("pagesize")]
    public int? PageSize { get; }

    public static MastMashupParams Create(
        string columns,
        List<MastMashupFilter> filters,
        string? position,
        double? radius,
        int? pageSize) =>
        new(columns, filters, position, radius, pageSize);
}
