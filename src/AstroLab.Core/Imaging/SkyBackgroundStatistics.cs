namespace AstroLab.Core.Imaging;

public readonly record struct SkyBackgroundStatistics(double Q1, double Q3, double SkySigma);

/// <summary>Static factory accompanying <see cref="SkyBackgroundStatistics"/>. Validates arguments before constructing.</summary>
public static class SkyBackgroundStatisticsFactory
{
    public static SkyBackgroundStatistics Create(double q1, double q3, double skySigma)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skySigma);

        return new SkyBackgroundStatistics(q1, q3, skySigma);
    }
}
