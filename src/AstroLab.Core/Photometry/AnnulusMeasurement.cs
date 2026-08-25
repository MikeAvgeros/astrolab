namespace AstroLab.Core.Photometry;

/// <summary>The result of estimating a local sky background from an annulus.</summary>
/// <param name="BackgroundPerPixel">The estimated background level per pixel.</param>
/// <param name="SampledPixelCount">The number of valid (finite) pixels included in the estimate.</param>
public readonly record struct AnnulusMeasurement(double BackgroundPerPixel, int SampledPixelCount);

/// <summary>Static factory accompanying <see cref="AnnulusMeasurement"/>. Validates arguments before constructing.</summary>
public static class AnnulusMeasurementFactory
{
    public static AnnulusMeasurement Create(double backgroundPerPixel, int sampledPixelCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sampledPixelCount);

        return new AnnulusMeasurement(backgroundPerPixel, sampledPixelCount);
    }
}
