namespace AstroLab.Api.Features.Spectroscopy.Lines;

public sealed record SpectralLineDto(double Wavelength, double Flux, double Fwhm);

/// <summary>Static factory accompanying <see cref="SpectralLineDto"/>. Validates arguments before constructing.</summary>
public static class SpectralLineDtoFactory
{
    public static SpectralLineDto Create(double wavelength, double flux, double fwhm)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fwhm);

        return new SpectralLineDto(wavelength, flux, fwhm);
    }
}
