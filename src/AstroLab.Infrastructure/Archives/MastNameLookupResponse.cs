using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed class MastNameLookupResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("resolvedCoordinate")]
    public List<MastResolvedCoordinate> ResolvedCoordinate { get; set; } = [];
}
