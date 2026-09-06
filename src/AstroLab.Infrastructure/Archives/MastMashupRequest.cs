using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record MastMashupRequest
{
    private MastMashupRequest(string service, string format, MastMashupParams parameters)
    {
        Service = service;
        Format = format;
        Params = parameters;
    }

    [JsonPropertyName("service")]
    public string Service { get; }

    [JsonPropertyName("format")]
    public string Format { get; }

    [JsonPropertyName("params")]
    public MastMashupParams Params { get; }

    public static MastMashupRequest Create(string service, MastMashupParams parameters) =>
        new(service, "json", parameters);
}
