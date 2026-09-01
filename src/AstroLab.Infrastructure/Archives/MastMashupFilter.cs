using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed class MastMashupFilter
{
    [JsonPropertyName("paramName")]
    public string ParamName { get; set; } = string.Empty;

    [JsonPropertyName("values")]
    public List<MastFilterValue> Values { get; set; } = [];
}
