using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record MastMashupRequest
{
    private const int FirstPage = 1;

    private MastMashupRequest(string service, string format, MastMashupParams parameters, int pageSize, int page)
    {
        Service = service;
        Format = format;
        Params = parameters;
        PageSize = pageSize;
        Page = page;
    }

    [JsonPropertyName("service")]
    public string Service { get; }

    [JsonPropertyName("format")]
    public string Format { get; }

    [JsonPropertyName("params")]
    public MastMashupParams Params { get; }
    
    [JsonPropertyName("pagesize")]
    public int PageSize { get; }

    [JsonPropertyName("page")]
    public int Page { get; }

    public static MastMashupRequest Create(string service, MastMashupParams parameters, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        return new MastMashupRequest(service, "json", parameters, pageSize, FirstPage);
    }
}
