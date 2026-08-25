namespace AstroLab.Api.Features.TimeSeries.PeriodSearch;

public sealed record PeriodSearchRequest(double MinPeriod, double MaxPeriod);

/// <summary>Static factory accompanying <see cref="PeriodSearchRequest"/>. Validates arguments before constructing.</summary>
public static class PeriodSearchRequestFactory
{
    public static PeriodSearchRequest Create(double minPeriod, double maxPeriod)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minPeriod);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPeriod);

        return new PeriodSearchRequest(minPeriod, maxPeriod);
    }
}
