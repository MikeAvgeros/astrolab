namespace AstroLab.Api.Features.TimeSeries.Transit;

public sealed record TransitRequest(double MinPeriod, double MaxPeriod, double MinTransitDepth);

/// <summary>Static factory accompanying <see cref="TransitRequest"/>. Validates arguments before constructing.</summary>
public static class TransitRequestFactory
{
    public static TransitRequest Create(double minPeriod, double maxPeriod, double minTransitDepth)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minPeriod);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPeriod);

        return new TransitRequest(minPeriod, maxPeriod, minTransitDepth);
    }
}
