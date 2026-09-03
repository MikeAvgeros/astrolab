using AstroLab.Core.Imaging;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Histogram;

/// <summary>Computes a pixel-value histogram for the first image-bearing HDU of a staged FITS file, suitable for client-side rendering.</summary>
public static class HistogramEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapHistogramEndpoint()
        {
            group.MapGet("/{fileId}/histogram", GetHistogramAsync)
                .WithSummary("Computes a pixel-value histogram for the first image-bearing HDU.");
        }
    }

    private static async Task<IResult> GetHistogramAsync(
        string fileId,
        FitsDatasetReader datasetReader,
        CancellationToken cancellationToken,
        int binCount = ImageStatistics.DefaultDisplayHistogramBinCount)
    {
        var request = HistogramRequest.Create(binCount);

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

        var histogramResult = ImageStatistics.ComputeHistogram(pixels, statsResult.Value, request.BinCount);

        return histogramResult.ToApiResult(histogram => Results.Ok(ImageHistogramResponse.FromHistogram(fileId, histogram)));
    }
}
