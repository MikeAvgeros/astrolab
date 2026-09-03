using AstroLab.Core.Astrometry;
using AstroLab.Core.Result;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Astrometry;

/// <summary>Converts between pixel and celestial (RA/Dec) coordinates via a staged image's FITS WCS, and reports the WCS solution itself.</summary>
public static class AstrometryEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapAstrometryEndpoint()
        {
            group.MapGet("/{fileId}/astrometry/wcs", GetWcsAsync)
                .WithSummary("Reports the WCS solution (coordinate system, projection, reference pixel/coordinates, pixel scale, rotation) for a staged image.");

            group.MapGet("/{fileId}/astrometry/pixel-to-world", ConvertPixelToWorldAsync)
                .WithSummary("Converts a pixel position to world (RA/Dec) coordinates via the image's WCS.");

            group.MapGet("/{fileId}/astrometry/world-to-pixel", ConvertWorldToPixelAsync)
                .WithSummary("Converts world (RA/Dec) coordinates to a pixel position via the image's WCS.");
        }
    }

    private static async Task<IResult> GetWcsAsync(string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var wcsResult = await LoadWcsAsync(fileId, datasetReader, cancellationToken);

        return wcsResult.ToApiResult(wcs => Results.Ok(WcsMetadataResponse.FromWcs(fileId, wcs)));
    }

    private static async Task<IResult> ConvertPixelToWorldAsync(
        string fileId, [AsParameters] PixelToWorldRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var wcsResult = await LoadWcsAsync(fileId, datasetReader, cancellationToken);

        if (wcsResult.IsFailure)
        {
            return wcsResult.Error.ToProblem();
        }

        var worldResult = wcsResult.Value.PixelToWorld(request.PixelX, request.PixelY);

        return worldResult.ToApiResult(world => Results.Ok(WorldCoordinateResponse.Create(fileId, world.RightAscension, world.Declination)));
    }

    private static async Task<IResult> ConvertWorldToPixelAsync(
        string fileId, [AsParameters] WorldToPixelRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var wcsResult = await LoadWcsAsync(fileId, datasetReader, cancellationToken);

        if (wcsResult.IsFailure)
        {
            return wcsResult.Error.ToProblem();
        }

        var pixelResult = wcsResult.Value.WorldToPixel(request.RightAscension, request.Declination);

        return pixelResult.ToApiResult(pixel => Results.Ok(PixelCoordinateResponse.Create(fileId, pixel.PixelX, pixel.PixelY)));
    }

    private static async Task<Result<Wcs>> LoadWcsAsync(string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return Result<Wcs>.Failure(datasetResult.Error);
        }

        using var dataset = datasetResult.Value;

        return Wcs.FromHeader(dataset.Hdu.Header);
    }
}
