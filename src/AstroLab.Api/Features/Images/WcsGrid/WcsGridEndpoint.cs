using System.Globalization;
using AstroLab.Core.Astrometry;
using AstroLab.Infrastructure.ImageRendering;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.WcsGrid;

/// <summary>Renders a staged image to PNG with a WCS right-ascension/declination coordinate grid overlaid.</summary>
public static class WcsGridEndpoint
{
    private const string PixelScaleXHeader = "X-Pixel-Scale-Arcsec-X";
    private const string PixelScaleYHeader = "X-Pixel-Scale-Arcsec-Y";
    private const string OrientationHeader = "X-Orientation-Degrees";
    private const string IsMirroredHeader = "X-Is-Mirrored";

    extension(IEndpointRouteBuilder group)
    {
        public void MapWcsGridEndpoint()
        {
            group.MapGet("/{fileId}/render/wcs-grid", GetWcsGridAsync)
                .WithSummary("Renders a staged image to PNG with a WCS right-ascension/declination coordinate grid overlaid.");
        }
    }

    private static async Task<IResult> GetWcsGridAsync(
        string fileId,
        FitsDatasetReader datasetReader,
        HttpContext httpContext,
        CancellationToken cancellationToken,
        int linesPerAxis = WcsGridGenerator.DefaultLinesPerAxis)
    {
        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var wcsResult = Wcs.FromHeader(dataset.Hdu.Header);

        if (wcsResult.IsFailure)
        {
            return wcsResult.Error.ToProblem();
        }

        var wcs = wcsResult.Value;

        var (width, height) = dataset.Image.Resolve2DDimensions();

        var gridResult = WcsGridGenerator.GenerateGridLines(wcs, width, height, linesPerAxis);

        if (gridResult.IsFailure)
        {
            return gridResult.Error.ToProblem();
        }

        var renderResult = FitsImageRenderer.Render(dataset.Pixels, width, height, RenderOptions.Create());

        if (renderResult.IsFailure)
        {
            return renderResult.Error.ToProblem();
        }

        var gridded = OverlayRenderer.DrawGridLines(renderResult.Value, gridResult.Value, width, height);

        var headers = httpContext.Response.Headers;

        headers[PixelScaleXHeader] = wcs.PixelScaleXArcsecPerPixel.ToString(CultureInfo.InvariantCulture);

        headers[PixelScaleYHeader] = wcs.PixelScaleYArcsecPerPixel.ToString(CultureInfo.InvariantCulture);

        headers[OrientationHeader] = wcs.RotationDegrees.ToString(CultureInfo.InvariantCulture);

        headers[IsMirroredHeader] = wcs.IsMirrored.ToString(CultureInfo.InvariantCulture);

        return Results.File(PngRenderer.Encode(gridded), "image/png");
    }
}
