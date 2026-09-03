namespace AstroLab.Core.Photometry;

public readonly record struct NetFluxMeasurement
{
    private NetFluxMeasurement(double rawFlux, double apertureArea, double backgroundPerPixel, double netFlux)
    {
        RawFlux = rawFlux;
        ApertureArea = apertureArea;
        BackgroundPerPixel = backgroundPerPixel;
        NetFlux = netFlux;
    }

    public double RawFlux { get; }

    public double ApertureArea { get; }

    public double BackgroundPerPixel { get; }

    public double NetFlux { get; }

    public double BackgroundSubtracted => RawFlux - BackgroundPerPixel * ApertureArea;

    public static NetFluxMeasurement Create(double rawFlux, double apertureArea, double backgroundPerPixel, double netFlux)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(apertureArea);

        return new NetFluxMeasurement(rawFlux, apertureArea, backgroundPerPixel, netFlux);
    }
}
