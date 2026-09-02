using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed class MastProductRecord
{
    [JsonPropertyName("dataURI")]
    public string? DataUri { get; set; }

    [JsonPropertyName("productFilename")]
    public string? ProductFilename { get; set; }

    [JsonPropertyName("productType")]
    public string? ProductType { get; set; }

    [JsonPropertyName("dataproduct_type")]
    public string? DataProductType { get; set; }

    [JsonPropertyName("calib_level")]
    public int? CalibLevel { get; set; }

    [JsonPropertyName("size")]
    public long? Size { get; set; }

    [JsonPropertyName("dataRights")]
    public string? DataRights { get; set; }
}
