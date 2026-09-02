using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed class MastNameLookupParams
{
    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;
}
