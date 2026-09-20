using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Spectroscopy.EquivalentWidth;

public sealed record EquivalentWidthRequest
{
    [JsonConstructor]
    private EquivalentWidthRequest(double minWavelength, double maxWavelength)
    {
        MinWavelength = minWavelength;
        MaxWavelength = maxWavelength;
    }

    /// <summary>In the file's native dispersion unit (its CRVAL1/CDELT1 solution — typically Ångström for optical spectra), not nanometers.</summary>
    public double MinWavelength { get; }

    /// <summary>In the file's native dispersion unit (its CRVAL1/CDELT1 solution — typically Ångström for optical spectra), not nanometers.</summary>
    public double MaxWavelength { get; }

    public static EquivalentWidthRequest Create(double minWavelength, double maxWavelength)
    {
        var request = new EquivalentWidthRequest(minWavelength, maxWavelength);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        if (MinWavelength >= MaxWavelength)
        {
            throw new ArgumentException("MinWavelength must be less than MaxWavelength.", nameof(MinWavelength));
        }
    }
}
