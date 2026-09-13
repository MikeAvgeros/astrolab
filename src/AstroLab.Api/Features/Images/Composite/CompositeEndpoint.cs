using AstroLab.Core.Imaging;
using AstroLab.Core.Result;
using AstroLab.Infrastructure.ImageRendering;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Composite;

/// <summary>Combines up to three staged images into an RGB colour composite, each channel independently auto-scaled.</summary>
public static class CompositeEndpoint
{
    private const double LowerPercentile = 1.0;
    private const double UpperPercentile = 99.0;

    extension(IEndpointRouteBuilder group)
    {
        public void MapCompositeEndpoint()
        {
            group.MapPost("/composite", CreateCompositeAsync)
                .WithSummary("Combines separate red/green/blue staged images into an RGB composite.");
        }
    }

    private static async Task<IResult> CreateCompositeAsync(CompositeRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        request.Validate();

        var redResult = await LoadChannelAsync(request.RedFileId, datasetReader, cancellationToken);

        if (redResult.IsFailure)
        {
            return redResult.Error.ToProblem();
        }

        var greenResult = await LoadChannelAsync(request.GreenFileId, datasetReader, cancellationToken);

        if (greenResult.IsFailure)
        {
            return greenResult.Error.ToProblem();
        }

        var blueResult = await LoadChannelAsync(request.BlueFileId, datasetReader, cancellationToken);

        if (blueResult.IsFailure)
        {
            return blueResult.Error.ToProblem();
        }

        var (redPixels, width, height) = redResult.Value;

        var (greenPixels, greenWidth, greenHeight) = greenResult.Value;

        var (bluePixels, blueWidth, blueHeight) = blueResult.Value;

        if (greenWidth != width || greenHeight != height || blueWidth != width || blueHeight != height)
        {
            return Error.Validation(
                "image.composite.dimension_mismatch",
                $"All three channel images must share the same dimensions; red is {width}x{height}, green is {greenWidth}x{greenHeight}, blue is {blueWidth}x{blueHeight}.")
                .ToProblem();
        }

        var redGrayscaleResult = ScaleChannelToGrayscale(redPixels);

        if (redGrayscaleResult.IsFailure)
        {
            return redGrayscaleResult.Error.ToProblem();
        }

        var greenGrayscaleResult = ScaleChannelToGrayscale(greenPixels);

        if (greenGrayscaleResult.IsFailure)
        {
            return greenGrayscaleResult.Error.ToProblem();
        }

        var blueGrayscaleResult = ScaleChannelToGrayscale(bluePixels);

        if (blueGrayscaleResult.IsFailure)
        {
            return blueGrayscaleResult.Error.ToProblem();
        }

        var composeResult = ImageCompositor.Compose(redGrayscaleResult.Value, greenGrayscaleResult.Value, blueGrayscaleResult.Value, width, height);

        return composeResult.ToApiResult(rendered => Results.File(PngRenderer.Encode(rendered), "image/png"));
    }

    private static async Task<Result<(float[] Pixels, int Width, int Height)>> LoadChannelAsync(
        string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return Result<(float[], int, int)>.Failure(datasetResult.Error);
        }

        using var dataset = datasetResult.Value;

        var (width, height) = dataset.Image.Resolve2DDimensions();

        return ([.. dataset.Pixels], width, height);
    }

    private static Result<byte[]> ScaleChannelToGrayscale(ReadOnlySpan<float> pixels)
    {
        var boundsResult = ImageStatistics.ComputePercentileBounds(pixels, LowerPercentile, UpperPercentile);

        if (boundsResult.IsFailure)
        {
            return Result<byte[]>.Failure(boundsResult.Error);
        }

        var (lower, upper) = boundsResult.Value;

        var parameters = ScaleParameters.Create(lower, upper, StretchMode.Asinh);

        var grayscale = new byte[pixels.Length];

        var stretchResult = ImageScaler.Stretch(pixels, grayscale, parameters);

        return stretchResult.IsFailure ? Result<byte[]>.Failure(stretchResult.Error) : grayscale;
    }
}
