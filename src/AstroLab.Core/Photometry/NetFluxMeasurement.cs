namespace AstroLab.Core.Photometry;

/// <summary>The result of a full aperture-photometry measurement: source flux net of local background.</summary>
public readonly record struct NetFluxMeasurement(
    double RawFlux,
    double ApertureArea,
    double BackgroundPerPixel,
    double NetFlux)
{
    public double BackgroundSubtracted => RawFlux - (BackgroundPerPixel * ApertureArea);
}
