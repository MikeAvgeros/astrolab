using System.Collections.Immutable;
using AstroLab.Core.Imaging;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Statistics;

/// <summary>Computes summary pixel statistics for the first image-bearing HDU of a staged FITS file.</summary>
public static class StatisticsEndpoint
{
    private const double MedianPercentile = 50.0;

    private static readonly ImmutableArray<double> DefaultPercentiles = [1.0, 5.0, 25.0, MedianPercentile, 75.0, 95.0, 99.0];

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

        Span<double> percentileValues = stackalloc double[DefaultPercentiles.Length];

        var percentilesResult = ImageStatistics.ComputePercentiles(pixels, stats, DefaultPercentiles.AsSpan(), percentileValues);

        if (percentilesResult.IsFailure)
        {
            return percentilesResult.Error.ToProblem();
        }

        var median = percentileValues[DefaultPercentiles.IndexOf(MedianPercentile)];

        var percentileDtos = new PercentileDto[DefaultPercentiles.Length];

        for (var i = 0; i < DefaultPercentiles.Length; i++)
        {
            percentileDtos[i] = PercentileDto.Create(DefaultPercentiles[i], percentileValues[i]);
        }

        return Results.Ok(ImageStatisticsResponse.Create(
            fileId, stats.Min, stats.Max, stats.Mean, median, stats.StdDev, stats.ValidPixelCount, stats.TotalPixelCount,
            stats.InvalidPixelCount, stats.DeadPixelPercentage, skyBackground.SkySigma, percentileDtos.ToImmutableList()));
    }
}
