using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record EsoColumnMetadata
{
    [JsonConstructor]
    private EsoColumnMetadata(string? name)
    {
        // System.Text.Json does not enforce non-null constructor parameters; a malformed TAP
        // response with a missing/null column name must not crash BuildColumnIndex's Name.ToLowerInvariant().
        Name = name ?? string.Empty;
    }

    [JsonPropertyName("name")]
    public string Name { get; }
}
