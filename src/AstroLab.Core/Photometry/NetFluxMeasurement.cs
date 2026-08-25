namespace AstroLab.Core.Photometry;

/// <summary>The result of a full aperture-photometry measurement: source flux net of local background.</summary>
public readonly record struct NetFluxMeasurement(double RawFlux, double ApertureArea, double BackgroundPerPixel, double NetFlux)
{
    public double BackgroundSubtracted => RawFlux - (BackgroundPerPixel * ApertureArea);
}

/// <summary>Static factory accompanying <see cref="NetFluxMeasurement"/>. Validates arguments before constructing.</summary>
public static class NetFluxMeasurementFactory
{
    public static NetFluxMeasurement Create(double rawFlux, double apertureArea, double backgroundPerPixel, double netFlux)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(apertureArea);

        return new NetFluxMeasurement(rawFlux, apertureArea, backgroundPerPixel, netFlux);
    }
}
