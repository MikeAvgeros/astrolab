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
        [AsParameters] RenderImageRequest request,
        FitsDatasetReader datasetReader,
        CancellationToken cancellationToken)
    {
        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);
        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        var dataset = datasetResult.Value;
        var (width, height) = dataset.Image.Resolve2DDimensions();
        var options = new RenderOptions(
            request.Stretch, request.AsinhSoftening, request.ColorMap, request.BlackPoint, request.WhitePoint, request.LowerPercentile, request.UpperPercentile);

        var pngResult = FitsImageRenderer.RenderToPng(dataset.Pixels, width, height, options);
        return pngResult.ToApiResult(png => Results.File(png, "image/png"));
    }
}
