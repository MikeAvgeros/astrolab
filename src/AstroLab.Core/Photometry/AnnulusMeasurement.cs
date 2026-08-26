namespace AstroLab.Core.Photometry;

/// <summary>The result of estimating a local sky background from an annulus.</summary>
public readonly record struct AnnulusMeasurement
{
    private AnnulusMeasurement(double backgroundPerPixel, int sampledPixelCount)
    {
        BackgroundPerPixel = backgroundPerPixel;
        SampledPixelCount = sampledPixelCount;
    }

    /// <summary>The estimated background level per pixel.</summary>
    public double BackgroundPerPixel { get; }

    /// <summary>The number of valid (finite) pixels included in the estimate.</summary>
    public int SampledPixelCount { get; }

    public static AnnulusMeasurement Create(double backgroundPerPixel, int sampledPixelCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sampledPixelCount);

        return new AnnulusMeasurement(backgroundPerPixel, sampledPixelCount);
    }
}
