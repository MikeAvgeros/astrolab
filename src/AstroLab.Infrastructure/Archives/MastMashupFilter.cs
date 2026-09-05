using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record MastMashupFilter
{
    [JsonConstructor]
    internal MastMashupFilter(string paramName, List<MastFilterValue> values)
    {
        ParamName = paramName;
        Values = values;
    }

    [JsonPropertyName("paramName")]
    public string ParamName { get; }

    [JsonPropertyName("values")]
    public List<MastFilterValue> Values { get; }

    public static MastMashupFilter Create(string paramName, List<MastFilterValue> values) => new(paramName, values);
}
