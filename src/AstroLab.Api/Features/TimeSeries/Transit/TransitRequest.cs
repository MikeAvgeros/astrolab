namespace AstroLab.Api.Features.TimeSeries.Transit;

public sealed record TransitRequest
{
    public TransitRequest(double minPeriod, double maxPeriod, double minTransitDepth)
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

        return new TransitRequest(minPeriod, maxPeriod, minTransitDepth);
    }
}
