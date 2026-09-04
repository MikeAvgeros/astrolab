namespace AstroLab.Api.Features.Measurements.StellarColour;

/// <summary>
/// Roadmap slice: measuring a star's brightness and colour index from aperture photometry across
/// two staged images taken in different bands. Request/response contract is final; the
/// measurement itself is not yet implemented (see spec.md §4.1), so this route always returns
/// HTTP 501.
/// </summary>
public static class StellarColourEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapStellarColourEndpoint()
        {
            group.MapPost("/{fileId}/stellar-colour", MeasureStellarColour)
                .WithSummary("Measures a star's brightness and colour index across two staged images in different bands. Not yet implemented.");
        }
    }

    private static IResult MeasureStellarColour(string fileId, StellarColourRequest request)
    {
        request.Validate();

        return NotImplementedResult.Value("measurements.stellarcolour.not_implemented", "Stellar colour measurement is not yet implemented.");
    }
}
