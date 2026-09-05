using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record MastProductResponse
{
    [JsonConstructor]
    internal MastProductResponse(string? status, string? msg, List<MastProductRecord>? data)
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
    public List<MastProductRecord> Data { get; }
}
