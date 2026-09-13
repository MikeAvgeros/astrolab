using System.Collections.Immutable;
using AstroLab.Core.Imaging;
using AstroLab.Core.Result;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Contours;

/// <summary>Generates contour level geometry (marching-squares polyline segments) from a staged image's pixel data.</summary>
public static class ContoursEndpoint
{
    private const int DefaultLevelCount = 5;

    extension(IEndpointRouteBuilder group)
    {
        public void MapContoursEndpoint()
        {
            group.MapGet("/{fileId}/contours", GetContoursAsync)
                .WithSummary("Generates scientific contour geometry from an image's pixel data, at configurable or automatically calculated levels.");
        }
    }

    private static async Task<IResult> GetContoursAsync(
        string fileId, double[]? levels, int? levelCount, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var request = ContoursRequest.Create(levels, levelCount);

        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var (width, height) = dataset.Image.Resolve2DDimensions();

        var levelsResult = ResolveLevels(request, dataset.Pixels);

        if (levelsResult.IsFailure)
        {
            return levelsResult.Error.ToProblem();
        }

        var levelDtos = ImmutableList.CreateBuilder<ContourLevelDto>();

        foreach (var level in levelsResult.Value)
        {
            var traceResult = ImageContourGenerator.Trace(dataset.Pixels, width, height, level);

            if (traceResult.IsFailure)
            {
                return traceResult.Error.ToProblem();
            }

            var polylines = traceResult.Value
                .Select(polyline => polyline.Select(point => ContourPointDto.Create(point.X, point.Y)).ToImmutableList())
                .ToImmutableList();

            levelDtos.Add(ContourLevelDto.Create(level, polylines));
        }

        return Results.Ok(ContoursResponse.Create(fileId, levelDtos.ToImmutable()));
    }

    private static Result<double[]> ResolveLevels(ContoursRequest request, ReadOnlySpan<float> pixels) =>
        request.Levels is { Length: > 0 } explicitLevels
            ? explicitLevels
            : ImageContourGenerator.SuggestLevels(pixels, request.LevelCount ?? DefaultLevelCount);
}
