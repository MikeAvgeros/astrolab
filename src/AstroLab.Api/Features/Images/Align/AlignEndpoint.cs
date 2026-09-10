using AstroLab.Core.Astrometry;
using AstroLab.Core.Result;
using AstroLab.Core.Sources;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Align;

/// <summary>
/// Computes the geometric transform (offset, rotation, scale) needed to register a target staged
/// image onto a reference staged image's pixel grid, using each image's WCS solution when both are
/// available, otherwise falling back to matching their detected source centroids.
/// </summary>
public static class AlignEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapAlignEndpoint()
        {
            group.MapPost("/align", AlignImagesAsync)
                .WithSummary("Computes the geometric transform to register one staged image onto another's pixel grid.");
        }
    }

    private static async Task<IResult> AlignImagesAsync(ImageAlignRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        request.Validate();

        var targetResult = await datasetReader.LoadImageAsync(request.FileId, cancellationToken);

        if (targetResult.IsFailure)
        {
            return targetResult.Error.ToProblem();
        }

        using var target = targetResult.Value;

        var referenceResult = await datasetReader.LoadImageAsync(request.ReferenceFileId, cancellationToken);

        if (referenceResult.IsFailure)
        {
            return referenceResult.Error.ToProblem();
        }

        using var reference = referenceResult.Value;

        var transformResult = ComputeTransform(target, reference);

        return transformResult.ToApiResult(transform => Results.Ok(ImageAlignResponse.Create(
            request.FileId, request.ReferenceFileId, transform.OffsetX, transform.OffsetY, transform.RotationDegrees, transform.Scale)));
    }

    private static Result<AlignmentTransform> ComputeTransform(FitsDataset target, FitsDataset reference)
    {
        var targetWcsResult = Wcs.FromHeader(target.Hdu.Header);

        var referenceWcsResult = Wcs.FromHeader(reference.Hdu.Header);

        if (targetWcsResult.IsSuccess && referenceWcsResult.IsSuccess)
        {
            return ImageAligner.AlignByWcs(targetWcsResult.Value, referenceWcsResult.Value);
        }

        var (targetWidth, targetHeight) = target.Image.Resolve2DDimensions();

        var (referenceWidth, referenceHeight) = reference.Image.Resolve2DDimensions();

        var targetSourcesResult = SourceDetector.Detect(target.Pixels, targetWidth, targetHeight);

        if (targetSourcesResult.IsFailure)
        {
            return Result<AlignmentTransform>.Failure(targetSourcesResult.Error);
        }

        var referenceSourcesResult = SourceDetector.Detect(reference.Pixels, referenceWidth, referenceHeight);

        if (referenceSourcesResult.IsFailure)
        {
            return Result<AlignmentTransform>.Failure(referenceSourcesResult.Error);
        }

        return ImageAligner.AlignBySourceCentroids(targetSourcesResult.Value, referenceSourcesResult.Value);
    }
}
