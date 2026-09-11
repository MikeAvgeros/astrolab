using AstroLab.Core.Result;

namespace AstroLab.Core.Photometry;

/// <summary>
/// Pure estimation of a star's effective temperature from a B-V colour index via the Ballesteros
/// (2012) analytic colour-temperature relation, a closed-form approximation to a black-body-based
/// calibration that avoids the lookup tables traditionally used for this conversion.
/// </summary>
public static class StellarTemperatureEstimator
{
    private const double ScaleKelvin = 4600.0;
    private const double FirstTermOffset = 1.7;
    private const double SecondTermOffset = 0.62;
    private const double ColourIndexWeight = 0.92;

    public static Result<double> EstimateKelvin(double colourIndex)
    {
        if (!double.IsFinite(colourIndex))
        {
            return Error.Validation("photometry.stellartemperature.invalid_colour_index", "colourIndex must be a finite value.");
        }

        var firstDenominator = ColourIndexWeight * colourIndex + FirstTermOffset;

        var secondDenominator = ColourIndexWeight * colourIndex + SecondTermOffset;

        if (firstDenominator == 0.0 || secondDenominator == 0.0)
        {
            return Error.Validation(
                "photometry.stellartemperature.singular_relation", "colourIndex produces a singular colour-temperature relation.");
        }

        var temperatureKelvin = ScaleKelvin * (1.0 / firstDenominator + 1.0 / secondDenominator);

        if (temperatureKelvin <= 0.0 || !double.IsFinite(temperatureKelvin))
        {
            return Error.Validation(
                "photometry.stellartemperature.out_of_range", "colourIndex does not produce a physically valid temperature estimate.");
        }

        return temperatureKelvin;
    }
}
