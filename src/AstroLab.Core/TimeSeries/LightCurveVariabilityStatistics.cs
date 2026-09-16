namespace AstroLab.Core.TimeSeries;

/// <summary>
/// Summary variability statistics for a light curve's flux series. <see cref="Rms"/> is the RMS
/// scatter about the mean, so it is numerically identical to <see cref="StandardDeviation"/>,
/// exposed under both names for convention.
/// </summary>
public readonly record struct LightCurveVariabilityStatistics
{
    private LightCurveVariabilityStatistics(double mean, double median, double standardDeviation, double amplitude, double rms, double medianAbsoluteDeviation)
    {
        Mean = mean;
        Median = median;
        StandardDeviation = standardDeviation;
        Amplitude = amplitude;
        Rms = rms;
        MedianAbsoluteDeviation = medianAbsoluteDeviation;
    }

    public double Mean { get; }

    public double Median { get; }

    public double StandardDeviation { get; }

    public double Amplitude { get; }

    public double Rms { get; }

    public double MedianAbsoluteDeviation { get; }

    public static LightCurveVariabilityStatistics Create(
        double mean, double median, double standardDeviation, double amplitude, double rms, double medianAbsoluteDeviation) =>
        new(mean, median, standardDeviation, amplitude, rms, medianAbsoluteDeviation);
}
