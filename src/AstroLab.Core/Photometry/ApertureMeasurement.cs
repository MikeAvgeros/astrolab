namespace AstroLab.Core.Photometry;

/// <summary>The result of integrating flux over a circular aperture.</summary>
public readonly record struct ApertureMeasurement
{
    private ApertureMeasurement(double flux, double area, int sampledPixelCount)
    {
        Flux = flux;
        Area = area;
        SampledPixelCount = sampledPixelCount;
    }

    /// <summary>The sum of pixel values weighted by their fractional coverage of the aperture.</summary>
    public double Flux { get; }

    /// <summary>The effective aperture area in pixels (sum of coverage fractions), which may be
    /// fractional and will be smaller than the nominal <c>π·r²</c> area near frame edges or invalid pixels.</summary>
    public double Area { get; }

    /// <summary>The number of distinct pixels that contributed non-zero coverage.</summary>
    public int SampledPixelCount { get; }

    public double MeanValue => Area > 0 ? Flux / Area : 0.0;

    public static ApertureMeasurement Create(double flux, double area, int sampledPixelCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(area);

        ArgumentOutOfRangeException.ThrowIfNegative(sampledPixelCount);

        return new ApertureMeasurement(flux, area, sampledPixelCount);
    }
}
