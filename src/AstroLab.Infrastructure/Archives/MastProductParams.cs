using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed record MastProductParams
{
    private MastProductParams(string obsId)
    {
        ObsId = obsId;
    }

    [JsonPropertyName("obsid")]
    public string ObsId { get; }

    public static MastProductParams Create(string obsId) => new(obsId);
}
