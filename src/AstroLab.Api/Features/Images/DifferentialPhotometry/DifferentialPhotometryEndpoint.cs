namespace AstroLab.Api.Features.Images.DifferentialPhotometry;

/// <summary>
/// Roadmap slice: differential photometry between a target aperture and a comparison aperture in
/// the same staged image, producing a differential magnitude. Request/response contract is
/// final; the algorithm itself is not yet implemented (see spec.md §4.1), so this route always
/// returns HTTP 501.
/// </summary>
public static class DifferentialPhotometryEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapDifferentialPhotometryEndpoint()
        {
            group.MapPost("/{fileId}/photometry/differential", MeasureDifferential)
                .WithSummary("Measures differential photometry between a target and comparison aperture. Not yet implemented.");
        }
    }

    private static IResult MeasureDifferential(string fileId, DifferentialPhotometryRequest request)
    {
        request.Validate();

        return NotImplementedResult.Value("images.differentialphotometry.not_implemented", "Differential photometry is not yet implemented.");
    }
}
