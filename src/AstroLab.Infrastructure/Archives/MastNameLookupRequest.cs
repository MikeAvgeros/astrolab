using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record MastNameLookupRequest
{
    private MastNameLookupRequest(string service, string format, MastNameLookupParams parameters)
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
    public MastNameLookupParams Params { get; }

    public static MastNameLookupRequest Create(string service, MastNameLookupParams parameters) =>
        new(service, "json", parameters);
}
