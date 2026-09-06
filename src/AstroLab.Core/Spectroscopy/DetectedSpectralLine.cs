namespace AstroLab.Core.Spectroscopy;

public readonly record struct DetectedSpectralLine
{
    private DetectedSpectralLine(double position, double flux, double fwhm)
    {
        Position = position;
        Flux = flux;
        Fwhm = fwhm;
    }

    public double Position { get; }

    public double Flux { get; }

    public double Fwhm { get; }

    public static DetectedSpectralLine Create(double position, double flux, double fwhm)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fwhm);

        return new DetectedSpectralLine(position, flux, fwhm);
    }
}
