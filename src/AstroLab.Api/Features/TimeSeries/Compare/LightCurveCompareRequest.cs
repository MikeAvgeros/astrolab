using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.TimeSeries.Compare;

public sealed record LightCurveCompareRequest
{
    [JsonConstructor]
    private LightCurveCompareRequest(ImmutableList<string> comparisonFileIds)
    {
        ComparisonFileIds = comparisonFileIds;
    }

    public ImmutableList<string> ComparisonFileIds { get; }

    public static LightCurveCompareRequest Create(ImmutableList<string> comparisonFileIds)
    {
        var request = new LightCurveCompareRequest(comparisonFileIds);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(ComparisonFileIds);

        if (ComparisonFileIds.IsEmpty)
        {
            throw new ArgumentException("At least one comparison file id is required.", nameof(ComparisonFileIds));
        }

        foreach (var comparisonFileId in ComparisonFileIds)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(comparisonFileId);
        }
    }
}
