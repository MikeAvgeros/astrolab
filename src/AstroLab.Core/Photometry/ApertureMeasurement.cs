namespace AstroLab.Core.Photometry;

public readonly record struct ApertureMeasurement
{
    private ApertureMeasurement(double flux, double area, int sampledPixelCount)
    {
        Flux = flux;
        Area = area;
        SampledPixelCount = sampledPixelCount;
    }

    public double Flux { get; }

    public double Area { get; }

    public int SampledPixelCount { get; }

    public double MeanValue => Area > 0 ? Flux / Area : 0.0;

    public static ApertureMeasurement Create(double flux, double area, int sampledPixelCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(area);

        ArgumentOutOfRangeException.ThrowIfNegative(sampledPixelCount);

        return new ApertureMeasurement(flux, area, sampledPixelCount);
    }
}
