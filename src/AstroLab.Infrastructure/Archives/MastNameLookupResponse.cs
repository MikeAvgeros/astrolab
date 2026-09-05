using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record MastNameLookupResponse
{
    [JsonConstructor]
    internal MastNameLookupResponse(string? status, List<MastResolvedCoordinate>? resolvedCoordinate)
    {
        Status = status ?? string.Empty;
        ResolvedCoordinate = resolvedCoordinate ?? [];
    }

    [JsonPropertyName("status")]
    public string Status { get; }

    [JsonPropertyName("resolvedCoordinate")]
    public List<MastResolvedCoordinate> ResolvedCoordinate { get; }
}
