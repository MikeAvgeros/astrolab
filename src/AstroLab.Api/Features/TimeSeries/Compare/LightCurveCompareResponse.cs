namespace AstroLab.Api.Features.TimeSeries.Compare;

public sealed record LightCurveCompareResponse
{
    private LightCurveCompareResponse(string fileId, string comparisonFileId, double correlationCoefficient, double meanMagnitudeDifference)
    {
        FileId = fileId;
        ComparisonFileId = comparisonFileId;
        CorrelationCoefficient = correlationCoefficient;
        MeanMagnitudeDifference = meanMagnitudeDifference;
    }

    public string FileId { get; }

    public string ComparisonFileId { get; }

    public double CorrelationCoefficient { get; }

    public double MeanMagnitudeDifference { get; }

    public static LightCurveCompareResponse Create(string fileId, string comparisonFileId, double correlationCoefficient, double meanMagnitudeDifference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentException.ThrowIfNullOrWhiteSpace(comparisonFileId);

        return new LightCurveCompareResponse(fileId, comparisonFileId, correlationCoefficient, meanMagnitudeDifference);
    }
}
