using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed class MastMashupParams
{
    [JsonPropertyName("columns")]
    public string Columns { get; set; } = "*";

    [JsonPropertyName("filters")]
    public List<MastMashupFilter> Filters { get; set; } = [];

    [JsonPropertyName("pagesize")]
    public int? PageSize { get; set; }
}
