using AstroLab.Core.Imaging;
using AstroLab.Core.Result;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Compare;

/// <summary>Compares and differences two staged, equally-sized images, e.g. to flag transient or variable sources.</summary>
public static class CompareEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapCompareEndpoint()
        {
            group.MapPost("/compare", CompareImagesAsync)
                .WithSummary("Compares and differences two staged images.");
        }
    }

    private static async Task<IResult> CompareImagesAsync(ImageCompareRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        request.Validate();

        var fileResult = await datasetReader.LoadImageAsync(request.FileId, cancellationToken);

        if (fileResult.IsFailure)
        {
            return fileResult.Error.ToProblem();
        }

        using var file = fileResult.Value;

        var comparisonResult = await datasetReader.LoadImageAsync(request.ComparisonFileId, cancellationToken);

        if (comparisonResult.IsFailure)
        {
            return comparisonResult.Error.ToProblem();
        }

        using var comparison = comparisonResult.Value;

        var (width, height) = file.Image.Resolve2DDimensions();

        var (comparisonWidth, comparisonHeight) = comparison.Image.Resolve2DDimensions();

        if (comparisonWidth != width || comparisonHeight != height)
        {
            return Error.Validation(
                "imaging.compare.invalid_image_bounds",
                $"Both images must share the same dimensions; the first is {width}x{height}, the comparison is {comparisonWidth}x{comparisonHeight}.")
                .ToProblem();
        }

        var differenceResult = ImageComparer.Compare(file.Pixels, comparison.Pixels, width, height);

        return differenceResult.ToApiResult(difference => Results.Ok(ImageCompareResponse.Create(
            request.FileId, request.ComparisonFileId, difference.MeanDifference, difference.StandardDeviationDifference, difference.MaxAbsoluteDifference)));
    }
}
