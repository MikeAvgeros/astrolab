using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed class MastProductRequest
{
    [JsonPropertyName("service")]
    public string Service { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; set; } = "json";

    [JsonPropertyName("params")]
    public MastProductParams Params { get; set; } = new();
}
