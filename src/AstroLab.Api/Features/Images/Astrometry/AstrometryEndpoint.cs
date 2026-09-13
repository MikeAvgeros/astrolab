using System.Collections.Immutable;
using AstroLab.Core.Astrometry;
using AstroLab.Core.Result;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Astrometry;

/// <summary>Converts between pixel and celestial (RA/Dec) coordinates via a staged image's FITS WCS, and reports the WCS solution, pixel scale, orientation, and validation diagnostics.</summary>
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
                .WithSummary("Reports the angular pixel scale (arcsec/pixel and degrees/pixel, per axis) derived from the image's WCS.");

            group.MapGet("/{fileId}/astrometry/orientation", GetOrientationAsync)
                .WithSummary("Reports the image's position angle relative to celestial north, and whether it is mirrored, derived from the image's WCS.");

            group.MapPost("/{fileId}/astrometry/pixel-to-world", ConvertPixelToWorldBatchAsync)
                .WithSummary("Converts multiple pixel positions to world (RA/Dec) coordinates in one request.");

            group.MapPost("/{fileId}/astrometry/world-to-pixel", ConvertWorldToPixelBatchAsync)
                .WithSummary("Converts multiple world (RA/Dec) coordinates to pixel positions in one request.");

            group.MapGet("/{fileId}/astrometry/validate", ValidateWcsAsync)
                .WithSummary("Validates the image's WCS solution: invertibility, axis orthogonality, pixel-scale symmetry, and pixel-to-world-to-pixel round-trip consistency.");
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

    private static async Task<IResult> GetPixelScaleAsync(string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var wcsResult = await LoadWcsAsync(fileId, datasetReader, cancellationToken);

        return wcsResult.ToApiResult(wcs => Results.Ok(PixelScaleResponse.Create(fileId, wcs)));
    }

    private static async Task<IResult> GetOrientationAsync(string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var wcsResult = await LoadWcsAsync(fileId, datasetReader, cancellationToken);

        return wcsResult.ToApiResult(wcs => Results.Ok(OrientationResponse.Create(fileId, wcs)));
    }

    private static async Task<IResult> ConvertPixelToWorldBatchAsync(
        string fileId, PixelToWorldBatchRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        request.Validate();

        var wcsResult = await LoadWcsAsync(fileId, datasetReader, cancellationToken);

        if (wcsResult.IsFailure)
        {
            return wcsResult.Error.ToProblem();
        }

        var wcs = wcsResult.Value;

        var pairs = ImmutableList.CreateBuilder<PixelWorldPairDto>();

        foreach (var point in request.Points)
        {
            var worldResult = wcs.PixelToWorld(point.PixelX, point.PixelY);

            if (worldResult.IsFailure)
            {
                return worldResult.Error.ToProblem();
            }

            pairs.Add(PixelWorldPairDto.Create(point.PixelX, point.PixelY, worldResult.Value.RightAscension, worldResult.Value.Declination));
        }

        return Results.Ok(PixelToWorldBatchResponse.Create(fileId, pairs.ToImmutable()));
    }

    private static async Task<IResult> ConvertWorldToPixelBatchAsync(
        string fileId, WorldToPixelBatchRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        request.Validate();

        var wcsResult = await LoadWcsAsync(fileId, datasetReader, cancellationToken);

        if (wcsResult.IsFailure)
        {
            return wcsResult.Error.ToProblem();
        }

        var wcs = wcsResult.Value;

        var pairs = ImmutableList.CreateBuilder<WorldPixelPairDto>();

        foreach (var point in request.Points)
        {
            var pixelResult = wcs.WorldToPixel(point.RightAscension, point.Declination);

            if (pixelResult.IsFailure)
            {
                return pixelResult.Error.ToProblem();
            }

            pairs.Add(WorldPixelPairDto.Create(point.RightAscension, point.Declination, pixelResult.Value.PixelX, pixelResult.Value.PixelY));
        }

        return Results.Ok(WorldToPixelBatchResponse.Create(fileId, pairs.ToImmutable()));
    }

    private static async Task<IResult> ValidateWcsAsync(string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var hduResult = await datasetReader.LoadImageMetadataAsync(fileId, cancellationToken);

        if (hduResult.IsFailure)
        {
            return hduResult.Error.ToProblem();
        }

        var hdu = hduResult.Value;

        var wcsResult = Wcs.FromHeader(hdu.Header);

        if (wcsResult.IsFailure)
        {
            return wcsResult.Error.ToProblem();
        }

        var (width, height) = hdu.Image!.Value.Resolve2DDimensions();

        var reportResult = WcsValidator.Validate(wcsResult.Value, width, height);

        return reportResult.ToApiResult(report => Results.Ok(WcsValidationResponse.Create(fileId, report)));
    }

    private static async Task<Result<Wcs>> LoadWcsAsync(string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var hduResult = await datasetReader.LoadImageMetadataAsync(fileId, cancellationToken);

        if (hduResult.IsFailure)
        {
            return Result<Wcs>.Failure(hduResult.Error);
        }

        return Wcs.FromHeader(hduResult.Value.Header);
    }
}
