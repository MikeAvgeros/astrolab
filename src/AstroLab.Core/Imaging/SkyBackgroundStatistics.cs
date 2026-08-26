namespace AstroLab.Core.Imaging;

public readonly record struct SkyBackgroundStatistics
{
    private SkyBackgroundStatistics(double q1, double q3, double skySigma)
    {
        Q1 = q1;
        Q3 = q3;
        SkySigma = skySigma;
    }

    public double Q1 { get; }

    public double Q3 { get; }

    public double SkySigma { get; }

    public static SkyBackgroundStatistics Create(double q1, double q3, double skySigma)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skySigma);

        return new SkyBackgroundStatistics(q1, q3, skySigma);
    }
}
