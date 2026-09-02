using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed class MastProductParams
{
    [JsonPropertyName("obsid")]
    public string ObsId { get; set; } = string.Empty;
}
