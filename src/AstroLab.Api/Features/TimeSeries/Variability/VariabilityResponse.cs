namespace AstroLab.Api.Features.TimeSeries.Variability;

public sealed record VariabilityResponse
{
    private VariabilityResponse(
        string fileId, double mean, double median, double standardDeviation, double amplitude, double rms, double medianAbsoluteDeviation)
    {
        FileId = fileId;
        Mean = mean;
        Median = median;
        StandardDeviation = standardDeviation;
        Amplitude = amplitude;
        Rms = rms;
        MedianAbsoluteDeviation = medianAbsoluteDeviation;
    }

    public string FileId { get; }

    public double Mean { get; }

    public double Median { get; }

    public double StandardDeviation { get; }

    public double Amplitude { get; }

    public double Rms { get; }

    public double MedianAbsoluteDeviation { get; }

    public static VariabilityResponse Create(
        string fileId, double mean, double median, double standardDeviation, double amplitude, double rms, double medianAbsoluteDeviation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new VariabilityResponse(fileId, mean, median, standardDeviation, amplitude, rms, medianAbsoluteDeviation);
    }
}
