using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed class MastCaomRecord
{
    [JsonPropertyName("obs_id")]
    public string? ObsId { get; set; }

    [JsonPropertyName("target_name")]
    public string? TargetName { get; set; }

    [JsonPropertyName("instrument_name")]
    public string? InstrumentName { get; set; }

    [JsonPropertyName("t_min")]
    public double? Min { get; set; }
}
