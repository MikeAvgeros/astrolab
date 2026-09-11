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

    public double MinWavelength { get; }

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
