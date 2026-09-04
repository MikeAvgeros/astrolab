using AstroLab.Core.Sources;

namespace AstroLab.Api.Features.Images.Segmentation;

/// <summary>
/// Roadmap slice: segmenting a staged image into per-source pixel masks for source isolation and
/// masking. Request/response contract is final; the segmentation algorithm itself is not yet
/// implemented (see spec.md §6.5), so this route always returns HTTP 501.
/// </summary>
public static class SegmentationEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapSegmentationEndpoint()
        {
            group.MapGet("/{fileId}/segmentation", SegmentImage)
                .WithSummary("Segments a staged image into per-source pixel masks. Not yet implemented.");
        }
    }

    private static IResult SegmentImage(
        string fileId,
        double thresholdSigma = SourceDetector.DefaultThresholdSigma,
        int minimumArea = SourceDetector.DefaultMinimumArea)
    {
        _ = SegmentationRequest.Create(thresholdSigma, minimumArea);

        return NotImplementedResult.Value("images.segmentation.not_implemented", "Image segmentation is not yet implemented.");
    }
}
