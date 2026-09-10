using System.Collections.Immutable;
using System.IO.Pipelines;
using AstroLab.Core.Imaging;
using AstroLab.Core.Result;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Stack;

/// <summary>
/// Combines multiple staged images of the same field into a single stacked image via mean,
/// median, sum, or sigma-clipped combination, staging the composite as a new derived file.
/// </summary>
public static class StackEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapStackEndpoint()
        {
            group.MapPost("/stack", StackImagesAsync)
                .WithSummary("Combines multiple staged images into a single stacked image.");
        }
    }

    private static async Task<IResult> StackImagesAsync(
        ImageStackRequest request, FitsDatasetReader datasetReader, ILocalFileStore fileStore, CancellationToken cancellationToken)
    {
        request.Validate();

        var framesResult = await LoadFramesAsync(request.FileIds, datasetReader, cancellationToken);

        if (framesResult.IsFailure)
        {
            return framesResult.Error.ToProblem();
        }

        var (frames, width, height) = framesResult.Value;

        var combineResult = ImageStacker.Combine(frames, width, height, request.Method);

        if (combineResult.IsFailure)
        {
            return combineResult.Error.ToProblem();
        }

        var fitsBytes = FitsImageWriter.WriteFloatImage(combineResult.Value, width, height);

        var resultFileId = fileStore.CreateStagingKey("fits");

        var writeResult = await fileStore.WriteAsync(resultFileId, PipeReader.Create(new MemoryStream(fitsBytes)), cancellationToken);

        return writeResult.ToApiResult(_ => Results.Ok(ImageStackResponse.Create(request.FileIds, request.Method, resultFileId)));
    }

    private static async Task<Result<(List<ReadOnlyMemory<float>> Frames, int Width, int Height)>> LoadFramesAsync(
        ImmutableList<string> fileIds, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var frames = new List<ReadOnlyMemory<float>>(fileIds.Count);

        var width = 0;

        var height = 0;

        var isFirstFrame = true;

        foreach (var fileId in fileIds)
        {
            var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

            if (datasetResult.IsFailure)
            {
                return Result<(List<ReadOnlyMemory<float>>, int, int)>.Failure(datasetResult.Error);
            }

            using var dataset = datasetResult.Value;

            var (frameWidth, frameHeight) = dataset.Image.Resolve2DDimensions();

            if (isFirstFrame)
            {
                width = frameWidth;

                height = frameHeight;

                isFirstFrame = false;
            }
            else if (frameWidth != width || frameHeight != height)
            {
                return Error.Validation(
                    "images.stack.dimension_mismatch",
                    $"All staged images must share the same dimensions to be stacked; expected {width}x{height} but '{fileId}' is {frameWidth}x{frameHeight}.");
            }

            frames.Add(dataset.Pixels.ToArray());
        }

        return (frames, width, height);
    }
}
