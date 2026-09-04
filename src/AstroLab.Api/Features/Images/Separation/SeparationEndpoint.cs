namespace AstroLab.Api.Features.Images.Separation;

/// <summary>
/// Roadmap slice: computing the angular separation between two pixel positions in a staged image
/// via its WCS solution. Request/response contract is final; the calculation itself is not yet
/// wired up (see spec.md §4.1), so this route always returns HTTP 501.
/// </summary>
public static class SeparationEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapSeparationEndpoint()
        {
            group.MapGet("/{fileId}/astrometry/separation", ComputeSeparation)
                .WithSummary("Computes the angular separation between two pixel positions via the image's WCS. Not yet implemented.");
        }
    }

    private static IResult ComputeSeparation(string fileId, double firstPixelX, double firstPixelY, double secondPixelX, double secondPixelY)
    {
        _ = AngularSeparationRequest.Create(firstPixelX, firstPixelY, secondPixelX, secondPixelY);

        return NotImplementedResult.Value("images.separation.not_implemented", "Angular separation calculation is not yet implemented.");
    }
}
