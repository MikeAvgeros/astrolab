using AstroLab.Core.Imaging;
using AstroLab.Infrastructure.ImageRendering;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Render;

/// <summary>Renders the first image-bearing HDU of a staged FITS file as a browser-displayable PNG.</summary>
public static class RenderEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapRenderEndpoint()
        {
            group.MapGet("/{fileId}/render", RenderAsync)
                .WithSummary("Renders the first image-bearing HDU of a staged FITS file as a PNG.");
        }
    }

    private static async Task<IResult> RenderAsync(
        string fileId,
        FitsDatasetReader datasetReader,
        CancellationToken cancellationToken,
        StretchMode stretch = StretchMode.Asinh,
        ColorMap colorMap = ColorMap.Grayscale,
        double? blackPoint = null,
        double? whitePoint = null,
        double lowerPercentile = RenderImageRequest.DefaultLowerPercentile,
        double upperPercentile = RenderImageRequest.DefaultUpperPercentile,
        double asinhSoftening = RenderImageRequest.DefaultAsinhSoftening,
        int? maxDimension = RenderOptions.DefaultMaxDimension)
    {
        var request = RenderImageRequest.Create(
            stretch, colorMap, blackPoint, whitePoint, lowerPercentile, upperPercentile, asinhSoftening, maxDimension);

        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var (width, height) = dataset.Image.Resolve2DDimensions();

        var options = RenderOptions.Create(
            request.Stretch, request.AsinhSoftening, request.ColorMap, request.BlackPoint, request.WhitePoint, request.LowerPercentile, request.UpperPercentile, request.MaxDimension);

        var pngResult = FitsImageRenderer.RenderToPng(dataset.Pixels, width, height, options);

        return pngResult.ToApiResult(png => Results.File(png, "image/png"));
    }
}
