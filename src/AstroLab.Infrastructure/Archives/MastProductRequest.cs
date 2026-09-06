using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record MastProductRequest
{
    private MastProductRequest(string service, string format, MastProductParams parameters)
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
    public MastProductParams Params { get; }

    public static MastProductRequest Create(string service, MastProductParams parameters) =>
        new(service, "json", parameters);
}
