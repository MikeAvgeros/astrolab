namespace AstroLab.Api.Features.TimeSeries.PeriodSearch;

public sealed record PeriodSearchRequest
{
    public PeriodSearchRequest(double minPeriod, double maxPeriod)
    {
        MinPeriod = minPeriod;
        MaxPeriod = maxPeriod;
    }

    public double MinPeriod { get; }

    public double MaxPeriod { get; }

    public static PeriodSearchRequest Create(double minPeriod, double maxPeriod)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minPeriod);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPeriod);

        return new PeriodSearchRequest(minPeriod, maxPeriod);
    }
}
