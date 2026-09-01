using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record EsoColumnMetadata(
    [property: JsonPropertyName("name")] string Name
);
