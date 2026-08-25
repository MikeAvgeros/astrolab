using AstroLab.Core.Imaging;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Statistics;

/// <summary>Computes summary pixel statistics for the first image-bearing HDU of a staged FITS file.</summary>
public static class StatisticsEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapStatisticsEndpoint()
        {
            group.MapGet("/{fileId}/statistics", GetStatisticsAsync)
                .WithSummary("Computes summary pixel statistics for the first image-bearing HDU.");
        }
    }

    private static async Task<IResult> GetStatisticsAsync(string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();

        }

        using var dataset = datasetResult.Value;

        var pixels = dataset.Pixels;

        var statsResult = ImageStatistics.Compute(pixels);

        if (statsResult.IsFailure)
        {
            return statsResult.Error.ToProblem();
        }

        var stats = statsResult.Value;

        var skyBackground = ImageStatistics.ComputeSkyBackground(pixels, stats);

        return Results.Ok(ImageStatisticsResponseFactory.Create(
            fileId, stats.Min, stats.Max, stats.Mean, stats.StdDev, stats.ValidPixelCount, stats.TotalPixelCount,
            stats.InvalidPixelCount, stats.DeadPixelPercentage, skyBackground.SkySigma));
    }
}
