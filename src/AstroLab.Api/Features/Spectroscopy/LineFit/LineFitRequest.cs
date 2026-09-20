using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Spectroscopy.LineFit;

public sealed record LineFitRequest
{
    [JsonConstructor]
    private LineFitRequest(
        double minWavelength, double maxWavelength,
        double? initialCentre, double? initialAmplitude, double? initialFwhm)
    {
        MinWavelength = minWavelength;
        MaxWavelength = maxWavelength;
        InitialCentre = initialCentre;
        InitialAmplitude = initialAmplitude;
        InitialFwhm = initialFwhm;
    }

    /// <summary>In the file's native dispersion unit (its CRVAL1/CDELT1 solution — typically Ångström for optical spectra), not nanometers.</summary>
    public double MinWavelength { get; }

    /// <summary>In the file's native dispersion unit (its CRVAL1/CDELT1 solution — typically Ångström for optical spectra), not nanometers.</summary>
    public double MaxWavelength { get; }

    public double? InitialCentre { get; }

    public double? InitialAmplitude { get; }

    public double? InitialFwhm { get; }

    public static LineFitRequest Create(
        double minWavelength, double maxWavelength,
        double? initialCentre = null, double? initialAmplitude = null, double? initialFwhm = null)
    {
        var request = new LineFitRequest(minWavelength, maxWavelength, initialCentre, initialAmplitude, initialFwhm);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        if (MinWavelength >= MaxWavelength)
        {
            throw new ArgumentException("MinWavelength must be less than MaxWavelength.", nameof(MinWavelength));
        }

        if (InitialFwhm is { } fwhm)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fwhm);
        }
    }
}
