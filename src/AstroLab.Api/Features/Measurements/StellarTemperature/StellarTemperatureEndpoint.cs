namespace AstroLab.Api.Features.Measurements.StellarTemperature;

/// <summary>
/// Roadmap slice: estimating a star's effective temperature from a colour index via a
/// colour-temperature relation. Request/response contract is final; the estimation itself is not
/// yet implemented (see spec.md §4.1), so this route always returns HTTP 501. The response is
/// always a model-derived estimate, never a direct measurement.
/// </summary>
public static class StellarTemperatureEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapStellarTemperatureEndpoint()
        {
            group.MapGet("/stellar-temperature", EstimateTemperature)
                .WithSummary("Estimates a star's effective temperature from a colour index. Not yet implemented.");
        }
    }

    private static IResult EstimateTemperature(double colourIndex)
    {
        _ = StellarTemperatureRequest.Create(colourIndex);

        return NotImplementedResult.Value("measurements.stellartemperature.not_implemented", "Stellar temperature estimation is not yet implemented.");
    }
}
