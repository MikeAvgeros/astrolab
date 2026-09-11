using AstroLab.Core.Photometry;

namespace AstroLab.Api.Features.Measurements.StellarTemperature;

/// <summary>
/// Estimates a star's effective temperature from a colour index via the Ballesteros (2012)
/// colour-temperature relation. The response is always a model-derived estimate, never a direct
/// measurement (see spec.md §6.5).
/// </summary>
public static class StellarTemperatureEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapStellarTemperatureEndpoint()
        {
            group.MapGet("/stellar-temperature", EstimateTemperature)
                .WithSummary("Estimates a star's effective temperature from a colour index.");
        }
    }

    private static IResult EstimateTemperature(double colourIndex)
    {
        var request = StellarTemperatureRequest.Create(colourIndex);

        var estimateResult = StellarTemperatureEstimator.EstimateKelvin(request.ColourIndex);

        return estimateResult.ToApiResult(temperatureKelvin =>
            Results.Ok(StellarTemperatureResponse.Create(request.ColourIndex, temperatureKelvin)));
    }
}
