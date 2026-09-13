namespace AstroLab.Api.Features.Images.ApertureCorrection;

public sealed record ApertureCorrectionResponse
{
    private ApertureCorrectionResponse(string fileId, double correctedFlux, double? correctedFluxUncertainty)
    {
        FileId = fileId;
        CorrectedFlux = correctedFlux;
        CorrectedFluxUncertainty = correctedFluxUncertainty;
    }

    public string FileId { get; }

    public double CorrectedFlux { get; }

    public double? CorrectedFluxUncertainty { get; }

    public static ApertureCorrectionResponse Create(string fileId, double correctedFlux, double? correctedFluxUncertainty)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new ApertureCorrectionResponse(fileId, correctedFlux, correctedFluxUncertainty);
    }
}
