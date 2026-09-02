using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed class MastNameLookupRequest
{
    [JsonPropertyName("service")]
    public string Service { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; set; } = "json";

    [JsonPropertyName("params")]
    public MastNameLookupParams Params { get; set; } = new();
}
