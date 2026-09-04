using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record EsoTapResponse
{
    [JsonConstructor]
    private EsoTapResponse(List<EsoColumnMetadata>? metadata, List<List<object>>? data)
    {
        Metadata = metadata;
        Data = data;
    }

    [JsonPropertyName("metadata")]
    public List<EsoColumnMetadata>? Metadata { get; }

    [JsonPropertyName("data")]
    public List<List<object>>? Data { get; }
}
