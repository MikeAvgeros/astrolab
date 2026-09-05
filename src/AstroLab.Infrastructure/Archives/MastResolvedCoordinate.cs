using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record MastResolvedCoordinate
{
    [JsonConstructor]
    internal MastResolvedCoordinate(string? canonicalName, double? rightAscension, double? declination)
    {
        CanonicalName = canonicalName;
        RightAscension = rightAscension;
        Declination = declination;
    }

    [JsonPropertyName("canonicalName")]
    public string? CanonicalName { get; }

    [JsonPropertyName("ra")]
    public double? RightAscension { get; }

    [JsonPropertyName("decl")]
    public double? Declination { get; }
}
