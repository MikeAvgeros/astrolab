using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Spectroscopy.Compare;

public sealed record SpectrumCompareRequest
{
    [JsonConstructor]
    private SpectrumCompareRequest(string comparisonFileId)
    {
        ComparisonFileId = comparisonFileId;
    }

    public string ComparisonFileId { get; }

    public static SpectrumCompareRequest Create(string comparisonFileId)
    {
        var request = new SpectrumCompareRequest(comparisonFileId);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ComparisonFileId);
    }
}
