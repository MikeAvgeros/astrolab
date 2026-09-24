using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record MastNameLookupParams
{
    private const string JsonFormat = "json";

    private MastNameLookupParams(string input, string format)
    {
        Input = input;
        Format = format;
    }

    [JsonPropertyName("input")]
    public string Input { get; }

    [JsonPropertyName("format")]
    public string Format { get; }

    public static MastNameLookupParams Create(string input) => new(input, JsonFormat);
}
