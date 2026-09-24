namespace AstroLab.Core.Photometry;

public readonly record struct NetFluxMeasurement
{
    private NetFluxMeasurement(double rawFlux, double apertureArea, double backgroundPerPixel, double netFlux, int backgroundPixelCount)
    {
        RawFlux = rawFlux;
        ApertureArea = apertureArea;
        BackgroundPerPixel = backgroundPerPixel;
        NetFlux = netFlux;
        BackgroundPixelCount = backgroundPixelCount;
    }

    public double RawFlux { get; }

    public double ApertureArea { get; }

    public double BackgroundPerPixel { get; }

    public double NetFlux { get; }

    public int BackgroundPixelCount { get; }

    public static NetFluxMeasurement Create(double rawFlux, double apertureArea, double backgroundPerPixel, double netFlux, int backgroundPixelCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(apertureArea);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(backgroundPixelCount);

        return new NetFluxMeasurement(rawFlux, apertureArea, backgroundPerPixel, netFlux, backgroundPixelCount);
    }
}
