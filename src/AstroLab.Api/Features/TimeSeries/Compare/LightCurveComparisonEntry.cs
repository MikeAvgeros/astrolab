namespace AstroLab.Api.Features.TimeSeries.Compare;

public sealed record LightCurveComparisonEntry
{
    private LightCurveComparisonEntry(
        string comparisonFileId,
        double correlationCoefficient,
        double meanMagnitudeDifference,
        double fluxRatio,
        double variabilityRatio,
        double? primaryBestPeriod,
        double? comparisonBestPeriod)
    {
        ComparisonFileId = comparisonFileId;
        CorrelationCoefficient = correlationCoefficient;
        MeanMagnitudeDifference = meanMagnitudeDifference;
        FluxRatio = fluxRatio;
        VariabilityRatio = variabilityRatio;
        PrimaryBestPeriod = primaryBestPeriod;
        ComparisonBestPeriod = comparisonBestPeriod;
    }

    public string ComparisonFileId { get; }

    public double CorrelationCoefficient { get; }

    public double MeanMagnitudeDifference { get; }

    public double FluxRatio { get; }

    public double VariabilityRatio { get; }

    public double? PrimaryBestPeriod { get; }
    
    public double? ComparisonBestPeriod { get; }
    
    public double? PeriodDifferenceFraction =>
        PrimaryBestPeriod is { } primary && ComparisonBestPeriod is { } comparison && primary != 0.0
            ? (comparison - primary) / primary
            : null;

    public static LightCurveComparisonEntry Create(
        string comparisonFileId,
        double correlationCoefficient,
        double meanMagnitudeDifference,
        double fluxRatio,
        double variabilityRatio,
        double? primaryBestPeriod,
        double? comparisonBestPeriod)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(comparisonFileId);

        return new LightCurveComparisonEntry(
            comparisonFileId, correlationCoefficient, meanMagnitudeDifference, fluxRatio, variabilityRatio, primaryBestPeriod, comparisonBestPeriod);
    }
}
