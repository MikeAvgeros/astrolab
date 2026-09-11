using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Images.ApertureCorrection;

public sealed record ApertureCorrectionRequest
{
    [JsonConstructor]
    private ApertureCorrectionRequest(double measuredFlux, double correctionFactor, double? measuredFluxUncertainty)
    {
        MeasuredFlux = measuredFlux;
        CorrectionFactor = correctionFactor;
        MeasuredFluxUncertainty = measuredFluxUncertainty;
    }

    public double MeasuredFlux { get; }

    public double CorrectionFactor { get; }

    public double? MeasuredFluxUncertainty { get; }

    public static ApertureCorrectionRequest Create(double measuredFlux, double correctionFactor, double? measuredFluxUncertainty = null)
    {
        var request = new ApertureCorrectionRequest(measuredFlux, correctionFactor, measuredFluxUncertainty);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(CorrectionFactor);

        if (MeasuredFluxUncertainty is { } uncertainty)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(uncertainty);
        }
    }
}
