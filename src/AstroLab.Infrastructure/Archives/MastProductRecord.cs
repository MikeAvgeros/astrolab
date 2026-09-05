using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record MastProductRecord
{
    [JsonConstructor]
    internal MastProductRecord(
        string? dataUri,
        string? productFilename,
        string? productType,
        string? dataProductType,
        int? calibLevel,
        long? size,
        string? dataRights)
    {
        DataUri = dataUri;
        ProductFilename = productFilename;
        ProductType = productType;
        DataProductType = dataProductType;
        CalibLevel = calibLevel;
        Size = size;
        DataRights = dataRights;
    }

    [JsonPropertyName("dataURI")]
    public string? DataUri { get; }

    [JsonPropertyName("productFilename")]
    public string? ProductFilename { get; }

    [JsonPropertyName("productType")]
    public string? ProductType { get; }

    [JsonPropertyName("dataproduct_type")]
    public string? DataProductType { get; }

    [JsonPropertyName("calib_level")]
    public int? CalibLevel { get; }

    [JsonPropertyName("size")]
    public long? Size { get; }

    [JsonPropertyName("dataRights")]
    public string? DataRights { get; }
}
