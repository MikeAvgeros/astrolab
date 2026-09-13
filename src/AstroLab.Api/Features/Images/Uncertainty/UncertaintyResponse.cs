namespace AstroLab.Api.Features.Images.Uncertainty;

public sealed record UncertaintyResponse
{
    private UncertaintyResponse(string fileId, double netFlux, double fluxUncertainty, double? detectorGainUsed)
    {
        FileId = fileId;
        NetFlux = netFlux;
        FluxUncertainty = fluxUncertainty;
        DetectorGainUsed = detectorGainUsed;
    }

    public string FileId { get; }

    public double NetFlux { get; }

    public double FluxUncertainty { get; }

    public double? DetectorGainUsed { get; }

    public static UncertaintyResponse Create(string fileId, double netFlux, double fluxUncertainty, double? detectorGainUsed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new UncertaintyResponse(fileId, netFlux, fluxUncertainty, detectorGainUsed);
    }
}
