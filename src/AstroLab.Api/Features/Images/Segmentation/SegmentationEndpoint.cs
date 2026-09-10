using System.Collections.Immutable;
using AstroLab.Core.Sources;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Segmentation;

/// <summary>Segments a staged image into per-source pixel regions using a mesh-based 2D background model, deblending regions with multiple significant peaks.</summary>
public static class SegmentationEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapSegmentationEndpoint()
        {
            group.MapGet("/{fileId}/segmentation", SegmentImageAsync)
                .WithSummary("Segments a staged image into per-source pixel regions using a 2D background model.");
        }
    }

    private static async Task<IResult> SegmentImageAsync(
        string fileId,
        FitsDatasetReader datasetReader,
        CancellationToken cancellationToken,
        double thresholdSigma = SourceDetector.DefaultThresholdSigma,
        int minimumArea = SourceDetector.DefaultMinimumArea)
    {
        var request = SegmentationRequest.Create(thresholdSigma, minimumArea);

        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var (width, height) = dataset.Image.Resolve2DDimensions();

        var segmentationResult = ImageSegmenter.Segment(dataset.Pixels, width, height, request.ThresholdSigma, request.MinimumArea);

        return segmentationResult.ToApiResult(segments => Results.Ok(SegmentationResponse.Create(
            fileId,
            segments
                .Select(segment => SegmentDto.Create(
                    segment.SegmentId, segment.PixelCount, segment.CentroidX, segment.CentroidY,
                    segment.MinX, segment.MinY, segment.MaxX, segment.MaxY))
                .ToImmutableList())));
    }
}
