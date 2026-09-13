namespace AstroLab.Api.Features.Images.Snr;

public sealed record SnrResponse
{
    private SnrResponse(string fileId, double netFlux, double fluxUncertainty, double signalToNoiseRatio)
    {
        FileId = fileId;
        NetFlux = netFlux;
        FluxUncertainty = fluxUncertainty;
        SignalToNoiseRatio = signalToNoiseRatio;
    }

    public string FileId { get; }

    public double NetFlux { get; }

    public double FluxUncertainty { get; }

    public double SignalToNoiseRatio { get; }

    public static SnrResponse Create(string fileId, double netFlux, double fluxUncertainty, double signalToNoiseRatio)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new SnrResponse(fileId, netFlux, fluxUncertainty, signalToNoiseRatio);
    }
}
