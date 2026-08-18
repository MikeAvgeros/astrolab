namespace AstroLab.Api.Features.Images.Sources;

/// <summary>
/// Roadmap slice: source detection over the primary image HDU. Request/response contract is
/// final; the detection algorithm itself is not yet implemented (see spec.md §4.1), so this route
/// always returns HTTP 501.
/// </summary>
public static class SourcesEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapSourcesEndpoint()
        {
            group.MapGet("/{fileId}/sources", DetectSources)
                .WithSummary("Detects point sources in the primary image HDU. Not yet implemented.");
        }
    }

    private static IResult DetectSources(string fileId, [AsParameters] SourceDetectionRequest request) =>
        NotImplementedResult.Value("images.sources.not_implemented", "Source detection is not yet implemented.");
}
