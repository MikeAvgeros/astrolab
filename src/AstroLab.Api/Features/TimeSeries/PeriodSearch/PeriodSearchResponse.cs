namespace AstroLab.Api.Features.TimeSeries.PeriodSearch;

public sealed record PeriodSearchResponse
{
    private PeriodSearchResponse(string fileId, double bestPeriod, double power)
    {
        FileId = fileId;
        BestPeriod = bestPeriod;
        Power = power;
    }

    public string FileId { get; }

    public double BestPeriod { get; }

    public double Power { get; }

    public static PeriodSearchResponse Create(string fileId, double bestPeriod, double power)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new PeriodSearchResponse(fileId, bestPeriod, power);
    }
}
