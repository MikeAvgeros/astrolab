namespace AstroLab.Api.Features.Measurements.StellarTemperature;

public sealed record StellarTemperatureResponse
{
    private StellarTemperatureResponse(double colourIndex, double estimatedTemperatureKelvin)
    {
        ColourIndex = colourIndex;
        EstimatedTemperatureKelvin = estimatedTemperatureKelvin;
    }

    public double ColourIndex { get; }

    public double EstimatedTemperatureKelvin { get; }

    public static StellarTemperatureResponse Create(double colourIndex, double estimatedTemperatureKelvin)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(estimatedTemperatureKelvin);

        return new StellarTemperatureResponse(colourIndex, estimatedTemperatureKelvin);
    }
}
