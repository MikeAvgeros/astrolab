namespace AstroLab.Api.Features.Measurements.GalaxyMorphology;

/// <summary>
/// Roadmap slice: estimating a galaxy's size, ellipticity, and morphological type from a staged
/// image. Request/response contract is final; the estimation algorithm itself is not yet
/// implemented (see spec.md §6.5), so this route always returns HTTP 501. The response is always
/// a model-derived estimate, never a direct measurement.
/// </summary>
public static class GalaxyMorphologyEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapGalaxyMorphologyEndpoint()
        {
            group.MapGet("/{fileId}/galaxy-morphology", EstimateMorphology)
                .WithSummary("Estimates a galaxy's size, ellipticity, and morphological type. Not yet implemented.");
        }
    }

    private static IResult EstimateMorphology(string fileId, double centerX, double centerY)
    {
        _ = GalaxyMorphologyRequest.Create(centerX, centerY);

        return NotImplementedResult.Value("measurements.galaxymorphology.not_implemented", "Galaxy morphology estimation is not yet implemented.");
    }
}
