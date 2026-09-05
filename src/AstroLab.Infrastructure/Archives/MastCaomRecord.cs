using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record MastCaomRecord
{
    [JsonConstructor]
    internal MastCaomRecord(
        string? obsId,
        string? targetName,
        string? obsCollection,
        string? instrumentName,
        string? dataProductType,
        int? calibLevel,
        double? min,
        double? max,
        double? exposureTime,
        double? rightAscension,
        double? declination,
        double? wavelengthMin,
        double? wavelengthMax,
        string? proposalId,
        string? proposalPi,
        string? dataRights)
    {
        ObsId = obsId;
        TargetName = targetName;
        ObsCollection = obsCollection;
        InstrumentName = instrumentName;
        DataProductType = dataProductType;
        CalibLevel = calibLevel;
        Min = min;
        Max = max;
        ExposureTime = exposureTime;
        RightAscension = rightAscension;
        Declination = declination;
        WavelengthMin = wavelengthMin;
        WavelengthMax = wavelengthMax;
        ProposalId = proposalId;
        ProposalPi = proposalPi;
        DataRights = dataRights;
    }

    [JsonPropertyName("obs_id")]
    public string? ObsId { get; }

    [JsonPropertyName("target_name")]
    public string? TargetName { get; }

    [JsonPropertyName("obs_collection")]
    public string? ObsCollection { get; }

    [JsonPropertyName("instrument_name")]
    public string? InstrumentName { get; }

    [JsonPropertyName("dataproduct_type")]
    public string? DataProductType { get; }

    [JsonPropertyName("calib_level")]
    public int? CalibLevel { get; }

    [JsonPropertyName("t_min")]
    public double? Min { get; }

    [JsonPropertyName("t_max")]
    public double? Max { get; }

    [JsonPropertyName("t_exptime")]
    public double? ExposureTime { get; }

    [JsonPropertyName("s_ra")]
    public double? RightAscension { get; }

    [JsonPropertyName("s_dec")]
    public double? Declination { get; }

    [JsonPropertyName("em_min")]
    public double? WavelengthMin { get; }

    [JsonPropertyName("em_max")]
    public double? WavelengthMax { get; }

    [JsonPropertyName("proposal_id")]
    public string? ProposalId { get; }

    [JsonPropertyName("proposal_pi")]
    public string? ProposalPi { get; }

    [JsonPropertyName("data_rights")]
    public string? DataRights { get; }
}
