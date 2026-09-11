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

            group.MapGet("/{fileId}/astrometry/pixel-scale", GetPixelScaleAsync)
                .WithSummary("Roadmap: reports the angular pixel scale (and per-axis scales) derived from the image's WCS. Not yet implemented (HTTP 501).");

            group.MapGet("/{fileId}/astrometry/orientation", GetOrientationAsync)
                .WithSummary("Roadmap: reports the image's position angle relative to celestial north, derived from the image's WCS. Not yet implemented (HTTP 501).");

            group.MapPost("/{fileId}/astrometry/pixel-to-world", ConvertPixelToWorldBatchAsync)
                .WithSummary("Roadmap: converts multiple pixel positions to world (RA/Dec) coordinates in one request. Not yet implemented (HTTP 501).");

            group.MapPost("/{fileId}/astrometry/world-to-pixel", ConvertWorldToPixelBatchAsync)
                .WithSummary("Roadmap: converts multiple world (RA/Dec) coordinates to pixel positions in one request. Not yet implemented (HTTP 501).");
        }
    }

    private static async Task<IResult> GetWcsAsync(string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var wcsResult = await LoadWcsAsync(fileId, datasetReader, cancellationToken);

        return wcsResult.ToApiResult(wcs => Results.Ok(WcsMetadataResponse.Create(fileId, wcs)));
    }

    private static async Task<IResult> ConvertPixelToWorldAsync(
        string fileId, double pixelX, double pixelY, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var request = PixelToWorldRequest.Create(pixelX, pixelY);

        var wcsResult = await LoadWcsAsync(fileId, datasetReader, cancellationToken);

        if (wcsResult.IsFailure)
        {
            return wcsResult.Error.ToProblem();
        }

        var worldResult = wcsResult.Value.PixelToWorld(request.PixelX, request.PixelY);

        return worldResult.ToApiResult(world => Results.Ok(WorldCoordinateResponse.Create(fileId, world.RightAscension, world.Declination)));
    }

    private static async Task<IResult> ConvertWorldToPixelAsync(
        string fileId, double rightAscension, double declination, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var request = WorldToPixelRequest.Create(rightAscension, declination);

        var wcsResult = await LoadWcsAsync(fileId, datasetReader, cancellationToken);

        if (wcsResult.IsFailure)
        {
            return wcsResult.Error.ToProblem();
        }

        var pixelResult = wcsResult.Value.WorldToPixel(request.RightAscension, request.Declination);

        return pixelResult.ToApiResult(pixel => Results.Ok(PixelCoordinateResponse.Create(fileId, pixel.PixelX, pixel.PixelY)));
    }

    private static Task<IResult> GetPixelScaleAsync(string fileId, CancellationToken cancellationToken) =>
        Task.FromResult(NotImplementedResult.Value(
            "astrometry.pixel_scale.not_implemented",
            "Pixel scale calculation is not yet implemented."));

    private static Task<IResult> GetOrientationAsync(string fileId, CancellationToken cancellationToken) =>
        Task.FromResult(NotImplementedResult.Value(
            "astrometry.orientation.not_implemented",
            "Image orientation calculation is not yet implemented."));

    private static Task<IResult> ConvertPixelToWorldBatchAsync(
        string fileId, PixelToWorldBatchRequest request, CancellationToken cancellationToken)
    {
        request.Validate();

        return Task.FromResult(NotImplementedResult.Value(
            "astrometry.pixel_to_world_batch.not_implemented",
            "Multi-point pixel-to-world conversion is not yet implemented."));
    }

    private static Task<IResult> ConvertWorldToPixelBatchAsync(
        string fileId, WorldToPixelBatchRequest request, CancellationToken cancellationToken)
    {
        request.Validate();

        return Task.FromResult(NotImplementedResult.Value(
            "astrometry.world_to_pixel_batch.not_implemented",
            "Multi-point world-to-pixel conversion is not yet implemented."));
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
