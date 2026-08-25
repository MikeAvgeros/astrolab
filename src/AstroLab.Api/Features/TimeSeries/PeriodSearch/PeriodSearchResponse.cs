namespace AstroLab.Api.Features.TimeSeries.PeriodSearch;

public sealed record PeriodSearchResponse(string FileId, double BestPeriod, double Power);

/// <summary>Static factory accompanying <see cref="PeriodSearchResponse"/>. Validates arguments before constructing.</summary>
public static class PeriodSearchResponseFactory
{
    public static PeriodSearchResponse Create(string fileId, double bestPeriod, double power)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new PeriodSearchResponse(fileId, bestPeriod, power);
    }
}
