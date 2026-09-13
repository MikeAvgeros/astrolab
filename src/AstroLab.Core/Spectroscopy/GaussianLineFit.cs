namespace AstroLab.Core.Spectroscopy;

/// <summary>Converged parameters, linearized-covariance uncertainties, and fit-quality summary for a single Gaussian-plus-baseline line fit (see <see cref="SpectralLineFitter"/>).</summary>
public readonly record struct GaussianLineFit
{
    private GaussianLineFit(
        double baseline, double baselineUncertainty,
        double amplitude, double amplitudeUncertainty,
        double center, double centerUncertainty,
        double fwhm, double fwhmUncertainty,
        double integratedFlux, double reducedChiSquare)
    {
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
    }

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

    public static GaussianLineFit Create(
        double baseline, double baselineUncertainty,
        double amplitude, double amplitudeUncertainty,
        double center, double centerUncertainty,
        double fwhm, double fwhmUncertainty,
        double integratedFlux, double reducedChiSquare) =>
        new(baseline, baselineUncertainty, amplitude, amplitudeUncertainty, center, centerUncertainty, fwhm, fwhmUncertainty, integratedFlux, reducedChiSquare);
}
