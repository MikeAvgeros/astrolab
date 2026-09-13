namespace AstroLab.Api.Features.Images.Photometry;

public sealed record AperturePhotometryResponse
{
    private AperturePhotometryResponse(
        string fileId, double rawFlux, double apertureArea, double backgroundPerPixel, double netFlux,
        double fluxUncertainty, double signalToNoiseRatio)
    {
        FileId = fileId;

        RawFlux = rawFlux;

        ApertureArea = apertureArea;

        BackgroundPerPixel = backgroundPerPixel;

        NetFlux = netFlux;

        FluxUncertainty = fluxUncertainty;

        SignalToNoiseRatio = signalToNoiseRatio;
    }

    public string FileId { get; }

    public double RawFlux { get; }

    public double ApertureArea { get; }

    public double BackgroundPerPixel { get; }

    public double NetFlux { get; }

    public double FluxUncertainty { get; }

    public double SignalToNoiseRatio { get; }

    public static AperturePhotometryResponse Create(
        string fileId, double rawFlux, double apertureArea, double backgroundPerPixel, double netFlux,
        double fluxUncertainty, double signalToNoiseRatio)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new AperturePhotometryResponse(fileId, rawFlux, apertureArea, backgroundPerPixel, netFlux, fluxUncertainty, signalToNoiseRatio);
    }
}
