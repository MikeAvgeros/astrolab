namespace AstroLab.Api.Features.TimeSeries.Transit;

public sealed record TransitRequest
{
    private TransitRequest(double minPeriod, double maxPeriod, double minTransitDepth)
    {
        MinPeriod = minPeriod;
        MaxPeriod = maxPeriod;
        MinTransitDepth = minTransitDepth;
    }

    public double MinPeriod { get; }

    public double MaxPeriod { get; }

    public double MinTransitDepth { get; }

    public static TransitRequest Create(double minPeriod, double maxPeriod, double minTransitDepth)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minPeriod);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPeriod);

        if (!double.IsFinite(minTransitDepth))
        {
            throw new ArgumentOutOfRangeException(nameof(minTransitDepth), minTransitDepth, "minTransitDepth must be finite.");
        }

        return new TransitRequest(minPeriod, maxPeriod, minTransitDepth);
    }
}
