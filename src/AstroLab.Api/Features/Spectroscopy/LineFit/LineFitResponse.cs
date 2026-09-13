namespace AstroLab.Api.Features.Spectroscopy.LineFit;

public sealed record LineFitResponse
{
    private LineFitResponse(
        string fileId,
        double baseline, double baselineUncertainty,
        double amplitude, double amplitudeUncertainty,
        double center, double centerUncertainty,
        double fwhm, double fwhmUncertainty,
        double integratedFlux, double reducedChiSquare,
        bool isWavelengthCalibrated)
    {
        FileId = fileId;
        Baseline = baseline;
        BaselineUncertainty = baselineUncertainty;
        Amplitude = amplitude;
        AmplitudeUncertainty = amplitudeUncertainty;
        Center = center;
        CenterUncertainty = centerUncertainty;
        Fwhm = fwhm;
        FwhmUncertainty = fwhmUncertainty;
        IntegratedFlux = integratedFlux;
        ReducedChiSquare = reducedChiSquare;
        IsWavelengthCalibrated = isWavelengthCalibrated;
    }

    public string FileId { get; }

    public double Baseline { get; }

    public double BaselineUncertainty { get; }

    public double Amplitude { get; }

    public double AmplitudeUncertainty { get; }

    public double Center { get; }

    public double CenterUncertainty { get; }

    public double Fwhm { get; }

    public double FwhmUncertainty { get; }

    public double IntegratedFlux { get; }

    public double ReducedChiSquare { get; }

    public bool IsWavelengthCalibrated { get; }

    public static LineFitResponse Create(
        string fileId,
        double baseline, double baselineUncertainty,
        double amplitude, double amplitudeUncertainty,
        double center, double centerUncertainty,
        double fwhm, double fwhmUncertainty,
        double integratedFlux, double reducedChiSquare,
        bool isWavelengthCalibrated)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new LineFitResponse(
            fileId, baseline, baselineUncertainty, amplitude, amplitudeUncertainty, center, centerUncertainty,
            fwhm, fwhmUncertainty, integratedFlux, reducedChiSquare, isWavelengthCalibrated);
    }
}
