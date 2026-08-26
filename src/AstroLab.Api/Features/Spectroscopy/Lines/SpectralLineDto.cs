namespace AstroLab.Api.Features.Spectroscopy.Lines;

public sealed record SpectralLineDto
{
    private SpectralLineDto(double wavelength, double flux, double fwhm)
    {
        Wavelength = wavelength;
        Flux = flux;
        Fwhm = fwhm;
    }

    public double Wavelength { get; }

    public double Flux { get; }

    public double Fwhm { get; }

    public static SpectralLineDto Create(double wavelength, double flux, double fwhm)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fwhm);

        return new SpectralLineDto(wavelength, flux, fwhm);
    }
}
