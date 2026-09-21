using AstroLab.Core.Astrometry;
using AstroLab.Core.Fits;
using AstroLab.Core.Imaging;
using AstroLab.Core.Result;
using AstroLab.Infrastructure.ImageRendering;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Cutout;

/// <summary>Extracts a rectangular (pixel-space or WCS-based sky-region) cutout from a staged image and renders it as a PNG.</summary>
public static class CutoutEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapCutoutEndpoint()
        {
            group.MapGet("/{fileId}/cutout", GetCutoutAsync)
                .WithSummary("Extracts a rectangular pixel region, or a WCS-based sky region, from a staged image and renders it as a PNG.");
        }
    }

    private static async Task<IResult> GetCutoutAsync(
        string fileId,
        int? x, int? y, int? width, int? height,
        double? rightAscension, double? declination, double? radiusArcseconds,
        FitsDatasetReader datasetReader,
        CancellationToken cancellationToken)
    {
        var request = CutoutRequest.Create(x, y, width, height, rightAscension, declination, radiusArcseconds);

        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var (imageWidth, imageHeight) = dataset.Image.Resolve2DDimensions();

        var hasPixelRegion = request.X.HasValue || request.Y.HasValue || request.Width.HasValue || request.Height.HasValue;

        var regionResult = hasPixelRegion
            ? ResolvePixelRegion(request, imageWidth, imageHeight)
            : ResolveSkyRegion(request, dataset.Hdu.Header, imageWidth, imageHeight);

        if (regionResult.IsFailure)
        {
            return regionResult.Error.ToProblem();
        }

        var (originX, originY, cutoutWidth, cutoutHeight) = regionResult.Value;

        var cutoutPixels = new float[cutoutWidth * cutoutHeight];

        var extractResult = ImageCutoutExtractor.ExtractRegion(
            dataset.Pixels, imageWidth, imageHeight, originX, originY, cutoutWidth, cutoutHeight, cutoutPixels);

        if (extractResult.IsFailure)
        {
            return extractResult.Error.ToProblem();
        }

        var pngResult = FitsImageRenderer.RenderToPng(cutoutPixels, cutoutWidth, cutoutHeight, RenderOptions.Create());

        return pngResult.ToApiResult(png => Results.File(png, "image/png"));
    }

    private static Result<(int X, int Y, int Width, int Height)> ResolvePixelRegion(CutoutRequest request, int imageWidth, int imageHeight)
    {
        var x = request.X ?? 0;

        var y = request.Y ?? 0;

        if (x < 0 || x >= imageWidth || y < 0 || y >= imageHeight)
        {
            return Error.Validation(
                "image.cutout.origin_out_of_bounds",
                $"Origin (x={x}, y={y}) lies outside the source image ({imageWidth}x{imageHeight}).");
        }

        var width = request.Width ?? imageWidth - x;

        var height = request.Height ?? imageHeight - y;

        if (width <= 0 || height <= 0 || width > imageWidth - x || height > imageHeight - y)
        {
            return Error.Validation(
                "image.cutout.out_of_bounds",
                $"Requested region (x={x}, y={y}, width={width}, height={height}) lies outside the source image ({imageWidth}x{imageHeight}).");
        }

        return (x, y, width, height);
    }

    private static Result<(int X, int Y, int Width, int Height)> ResolveSkyRegion(
        CutoutRequest request, FitsHeader header, int imageWidth, int imageHeight)
    {
        if (request.RightAscension is not { } rightAscension || request.Declination is not { } declination ||
            request.RadiusArcseconds is not { } radiusArcseconds)
        {
            return Error.Validation(
                "image.cutout.incomplete_sky_region",
                "A sky-region cutout requires rightAscension, declination, and radiusArcseconds all to be supplied.");
        }

        var wcsResult = Wcs.FromHeader(header);

        if (wcsResult.IsFailure)
        {
            return Result<(int, int, int, int)>.Failure(wcsResult.Error);
        }

        return wcsResult.Value.ResolveSkyRegionPixelBounds(rightAscension, declination, radiusArcseconds, imageWidth, imageHeight);
    }
}
