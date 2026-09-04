namespace AstroLab.Api.Features.Images.Footprint;

/// <summary>
/// Roadmap slice: reporting the sky footprint (corner RA/Dec) of a staged image, derived from its
/// WCS and pixel dimensions. Response contract is final; the calculation itself is not yet wired
/// up (see spec.md §4.1), so this route always returns HTTP 501.
/// </summary>
public static class FootprintEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapFootprintEndpoint()
        {
            group.MapGet("/{fileId}/astrometry/footprint", GetFootprint)
                .WithSummary("Reports the sky footprint (corner RA/Dec) of a staged image. Not yet implemented.");
        }
    }

    private static IResult GetFootprint(string fileId) =>
        NotImplementedResult.Value("images.footprint.not_implemented", "Image sky footprint calculation is not yet implemented.");
}
