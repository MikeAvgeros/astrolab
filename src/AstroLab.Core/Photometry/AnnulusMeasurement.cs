namespace AstroLab.Core.Photometry;

public readonly record struct AnnulusMeasurement
{
    private AnnulusMeasurement(double backgroundPerPixel, int sampledPixelCount)
    {
        BackgroundPerPixel = backgroundPerPixel;
        SampledPixelCount = sampledPixelCount;
    }

    public double BackgroundPerPixel { get; }

    public int SampledPixelCount { get; }

    public static AnnulusMeasurement Create(double backgroundPerPixel, int sampledPixelCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sampledPixelCount);

        return new AnnulusMeasurement(backgroundPerPixel, sampledPixelCount);
    }
}
