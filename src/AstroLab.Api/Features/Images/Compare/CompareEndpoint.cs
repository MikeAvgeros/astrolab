using AstroLab.Core.Imaging;
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

        var differenceResult = ImageComparer.Compare(file.Pixels, comparison.Pixels, width, height);

        return differenceResult.ToApiResult(difference => Results.Ok(ImageCompareResponse.Create(
            request.FileId, request.ComparisonFileId, difference.MeanDifference, difference.StandardDeviationDifference, difference.MaxAbsoluteDifference)));
    }
}
