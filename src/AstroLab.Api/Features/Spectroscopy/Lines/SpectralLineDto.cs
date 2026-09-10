namespace AstroLab.Api.Features.Spectroscopy.Lines;

public sealed record SpectralLineDto
{
    private SpectralLineDto(double wavelength, double flux, double fwhm, double binPosition, bool isWavelengthCalibrated, bool isEmission)
    {
        Wavelength = wavelength;
        Flux = flux;
        Fwhm = fwhm;
        BinPosition = binPosition;
        IsWavelengthCalibrated = isWavelengthCalibrated;
        IsEmission = isEmission;
    }

    public double Wavelength { get; }

    public double Flux { get; }

    public double Fwhm { get; }

    public double BinPosition { get; }

    public bool IsWavelengthCalibrated { get; }
    
    public bool IsEmission { get; }

    public static SpectralLineDto Create(double wavelength, double flux, double fwhm, double binPosition, bool isWavelengthCalibrated)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fwhm);

        return new SpectralLineDto(wavelength, flux, fwhm, binPosition, isWavelengthCalibrated, isEmission: flux > 0.0);
    }
}
