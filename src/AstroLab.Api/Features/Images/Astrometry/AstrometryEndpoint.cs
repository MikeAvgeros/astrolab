namespace AstroLab.Api.Features.Images.Astrometry;

/// <summary>
/// Roadmap slice: pixel-to-world coordinate transformation via the image's WCS. Request/response
/// contract is final; the WCS solver itself is not yet implemented (see spec.md §4.1), so this
/// route always returns HTTP 501.
/// </summary>
public static class AstrometryEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapAstrometryEndpoint()
        {
            group.MapGet("/{fileId}/astrometry/pixel-to-world", ConvertPixelToWorld)
                .WithSummary("Converts a pixel position to world (RA/Dec) coordinates via the image's WCS. Not yet implemented.");
        }
    }

    private static IResult ConvertPixelToWorld(string fileId, [AsParameters] PixelToWorldRequest request) =>
        NotImplementedResult.Value("images.astrometry.not_implemented", "WCS pixel-to-world conversion is not yet implemented.");
}
