using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record EsoTapResponse(
    [property: JsonPropertyName("metadata")] List<EsoColumnMetadata>? Metadata,
    [property: JsonPropertyName("data")] List<List<object>>? Data
);
