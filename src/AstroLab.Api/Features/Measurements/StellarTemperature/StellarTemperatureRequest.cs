namespace AstroLab.Api.Features.Measurements.StellarTemperature;

public sealed record StellarTemperatureRequest
{
    private StellarTemperatureRequest(double colourIndex)
    {
        ColourIndex = colourIndex;
    }

    public double ColourIndex { get; }

    public static StellarTemperatureRequest Create(double colourIndex) =>
        new(colourIndex);
}
