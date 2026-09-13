using System.Collections.Immutable;

namespace AstroLab.Api.Features.TimeSeries.PeriodSearch;

public sealed record PeriodSearchResponse
{
    private PeriodSearchResponse(
        string fileId, double bestPeriod, double power, ImmutableList<double> periods, ImmutableList<double> powers, double falseAlarmProbability)
    {
        FileId = fileId;
        BestPeriod = bestPeriod;
        Power = power;
        Periods = periods;
        Powers = powers;
        FalseAlarmProbability = falseAlarmProbability;
    }

    public string FileId { get; }

    public double BestPeriod { get; }

    public double Power { get; }

    public ImmutableList<double> Periods { get; }

    public ImmutableList<double> Powers { get; }

    public double FalseAlarmProbability { get; }

    public static PeriodSearchResponse Create(
        string fileId, double bestPeriod, double power, ImmutableList<double> periods, ImmutableList<double> powers, double falseAlarmProbability)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new PeriodSearchResponse(fileId, bestPeriod, power, periods, powers, falseAlarmProbability);
    }
}
