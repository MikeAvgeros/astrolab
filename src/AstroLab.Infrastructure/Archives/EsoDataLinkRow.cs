using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

/// <summary>
/// One row of ESO's DataLink <c>links</c> response. With <c>RESPONSEFORMAT=json</c> ESO returns a
/// JSON array of these objects rather than the TAP <c>metadata</c>/<c>data</c> table shape.
/// </summary>
internal sealed record EsoDataLinkRow
{
    [JsonConstructor]
    private EsoDataLinkRow(
        string? id, string? accessUrl, string? errorMessage, string? semantics, string? contentType,
        long? contentLength, string? esoOrigFile)
    {
        Id = id;
        AccessUrl = accessUrl;
        ErrorMessage = errorMessage;
        Semantics = semantics;
        ContentType = contentType;
        ContentLength = contentLength;
        EsoOrigFile = esoOrigFile;
    }

    [JsonPropertyName("id")]
    public string? Id { get; }

    [JsonPropertyName("access_url")]
    public string? AccessUrl { get; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; }

    [JsonPropertyName("semantics")]
    public string? Semantics { get; }

    [JsonPropertyName("content_type")]
    public string? ContentType { get; }

    [JsonPropertyName("content_length")]
    public long? ContentLength { get; }

    [JsonPropertyName("eso_origfile")]
    public string? EsoOrigFile { get; }
}
