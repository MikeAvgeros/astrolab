using AstroLab.Core.Imaging;
using AstroLab.Infrastructure.ImageRendering;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Imaging;

/// <summary>FITS-to-browser-image visualization endpoints: PNG rendering and pixel statistics.</summary>
public static class ImagingEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public RouteGroupBuilder MapImagingEndpoints()
        {
            var group = app.MapGroup("/api/imaging").WithTags("Imaging");

            group.MapGet("/{fileId}/render", RenderAsync)
                .WithSummary("Renders the primary image HDU of a staged FITS file as a PNG.");

            group.MapGet("/{fileId}/statistics", GetStatisticsAsync)
                .WithSummary("Computes summary pixel statistics for the primary image HDU.");

            return group;
        }
    }

    private static async Task<IResult> RenderAsync(
        string fileId,
        FitsDatasetReader datasetReader,
        FitsImageRenderer renderer,
        CancellationToken cancellationToken,
        StretchMode stretch = StretchMode.Asinh,
        ColorMap colorMap = ColorMap.Grayscale,
        double? blackPoint = null,
        double? whitePoint = null,
        double lowerPercentile = 1.0,
        double upperPercentile = 99.0,
        double asinhSoftening = 0.1)
    {
        var datasetResult = await datasetReader.LoadPrimaryImageAsync(fileId, cancellationToken);
        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        var dataset = datasetResult.Value;
        var (width, height) = dataset.Image.Resolve2DDimensions();
        var options = new RenderOptions(stretch, asinhSoftening, colorMap, blackPoint, whitePoint, lowerPercentile, upperPercentile);

        var pngResult = renderer.RenderToPng(dataset.Pixels, width, height, options);
        return pngResult.ToApiResult(png => Results.File(png, "image/png"));
    }

    private static async Task<IResult> GetStatisticsAsync(string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var datasetResult = await datasetReader.LoadPrimaryImageAsync(fileId, cancellationToken);
        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        var statsResult = ImageStatistics.Compute(datasetResult.Value.Pixels);
        return statsResult.ToApiResult(stats => Results.Ok(new ImageStatisticsResponse(
            fileId, stats.Min, stats.Max, stats.Mean, stats.StdDev, stats.ValidPixelCount, stats.TotalPixelCount)));
    }
}
