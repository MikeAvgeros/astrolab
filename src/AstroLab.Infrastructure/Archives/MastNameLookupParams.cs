using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record MastNameLookupParams
{
    [JsonConstructor]
    internal MastNameLookupParams(string input)
    {
        Input = input;
    }

    [JsonPropertyName("input")]
    public string Input { get; }

    public static MastNameLookupParams Create(string input) => new(input);
}
