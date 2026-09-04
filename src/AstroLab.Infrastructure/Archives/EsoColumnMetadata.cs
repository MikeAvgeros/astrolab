using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record EsoColumnMetadata
{
    [JsonConstructor]
    private EsoColumnMetadata(string name)
    {
        Name = name;
    }

    [JsonPropertyName("name")]
    public string Name { get; }
}
