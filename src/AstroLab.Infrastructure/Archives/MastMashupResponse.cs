using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record MastMashupResponse
{
    [JsonConstructor]
    internal MastMashupResponse(string? status, string? msg, List<MastCaomRecord>? data)
    {
        Status = status ?? string.Empty;
        Msg = msg;
        Data = data ?? [];
    }

    [JsonPropertyName("status")]
    public string Status { get; }

    [JsonPropertyName("msg")]
    public string? Msg { get; }

    [JsonPropertyName("data")]
    public List<MastCaomRecord> Data { get; }
}
