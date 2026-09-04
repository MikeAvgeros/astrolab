using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.TimeSeries.Compare;

public sealed record LightCurveCompareRequest
{
    [JsonConstructor]
    private LightCurveCompareRequest(string comparisonFileId)
    {
        ComparisonFileId = comparisonFileId;
    }

    public string ComparisonFileId { get; }

    public static LightCurveCompareRequest Create(string comparisonFileId)
    {
        var request = new LightCurveCompareRequest(comparisonFileId);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ComparisonFileId);
    }
}
