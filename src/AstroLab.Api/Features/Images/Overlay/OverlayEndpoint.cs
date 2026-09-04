using AstroLab.Core.Sources;

namespace AstroLab.Api.Features.Images.Overlay;

/// <summary>
/// Roadmap slice: rendering a staged image to PNG with detected sources overlaid as markers.
/// Request contract is final; the overlay rendering itself is not yet implemented (see
/// spec.md §4.1), so this route always returns HTTP 501.
/// </summary>
public static class OverlayEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapOverlayEndpoint()
        {
            group.MapGet("/{fileId}/render/overlay", RenderOverlay)
                .WithSummary("Renders a staged image to PNG with detected sources overlaid. Not yet implemented.");
        }
    }

    private static IResult RenderOverlay(
        string fileId,
        double thresholdSigma = SourceDetector.DefaultThresholdSigma,
        int minimumArea = SourceDetector.DefaultMinimumArea,
        int maxSources = SourceDetector.DefaultMaxSources)
    {
        _ = OverlayRenderRequest.Create(thresholdSigma, minimumArea, maxSources);

        return NotImplementedResult.Value("images.overlay.not_implemented", "Source overlay rendering is not yet implemented.");
    }
}
