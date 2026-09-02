using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed class MastCaomRecord
{
    [JsonPropertyName("obs_id")]
    public string? ObsId { get; set; }

    [JsonPropertyName("target_name")]
    public string? TargetName { get; set; }

    [JsonPropertyName("obs_collection")]
    public string? ObsCollection { get; set; }

    [JsonPropertyName("instrument_name")]
    public string? InstrumentName { get; set; }

    [JsonPropertyName("dataproduct_type")]
    public string? DataProductType { get; set; }

    [JsonPropertyName("calib_level")]
    public int? CalibLevel { get; set; }

    [JsonPropertyName("t_min")]
    public double? Min { get; set; }

    [JsonPropertyName("t_max")]
    public double? Max { get; set; }

    [JsonPropertyName("t_exptime")]
    public double? ExposureTime { get; set; }

    [JsonPropertyName("s_ra")]
    public double? RightAscension { get; set; }

    [JsonPropertyName("s_dec")]
    public double? Declination { get; set; }

    [JsonPropertyName("em_min")]
    public double? WavelengthMin { get; set; }

    [JsonPropertyName("em_max")]
    public double? WavelengthMax { get; set; }

    [JsonPropertyName("proposal_id")]
    public string? ProposalId { get; set; }

    [JsonPropertyName("proposal_pi")]
    public string? ProposalPi { get; set; }

    [JsonPropertyName("data_rights")]
    public string? DataRights { get; set; }
}
