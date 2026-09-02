using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed class MastResolvedCoordinate
{
    [JsonPropertyName("canonicalName")]
    public string? CanonicalName { get; set; }

    [JsonPropertyName("ra")]
    public double? RightAscension { get; set; }

    [JsonPropertyName("decl")]
    public double? Declination { get; set; }
}
