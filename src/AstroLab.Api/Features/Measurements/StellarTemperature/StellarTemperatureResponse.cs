namespace AstroLab.Api.Features.Measurements.StellarTemperature;

public sealed record StellarTemperatureResponse
{
    private StellarTemperatureResponse(double colourIndex, double estimatedTemperatureKelvin, string method)
    {
        ColourIndex = colourIndex;
        EstimatedTemperatureKelvin = estimatedTemperatureKelvin;
        Method = method;
    }

    public double ColourIndex { get; }

    public double EstimatedTemperatureKelvin { get; }

    public string Method { get; }

    public static StellarTemperatureResponse Create(double colourIndex, double estimatedTemperatureKelvin, string method)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(estimatedTemperatureKelvin);

        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        return new StellarTemperatureResponse(colourIndex, estimatedTemperatureKelvin, method);
    }
}
