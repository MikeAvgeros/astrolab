namespace AstroLab.Core.Photometry;

/// <summary>The result of integrating flux over a circular aperture.</summary>
/// <param name="Flux">The sum of pixel values weighted by their fractional coverage of the aperture.</param>
/// <param name="Area">The effective aperture area in pixels (sum of coverage fractions), which may be
/// fractional and will be smaller than the nominal <c>π·r²</c> area near frame edges or invalid pixels.</param>
/// <param name="SampledPixelCount">The number of distinct pixels that contributed non-zero coverage.</param>
public readonly record struct ApertureMeasurement(double Flux, double Area, int SampledPixelCount)
{
    public double MeanValue => Area > 0 ? Flux / Area : 0.0;
}
