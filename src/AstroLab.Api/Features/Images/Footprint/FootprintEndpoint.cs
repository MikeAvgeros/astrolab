using System.Collections.Immutable;
using AstroLab.Core.Astrometry;
using AstroLab.Core.Result;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Footprint;

/// <summary>Reports the sky footprint (corner RA/Dec) of a staged image, derived from its WCS and pixel dimensions.</summary>
public static class FootprintEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapFootprintEndpoint()
        {
            group.MapGet("/{fileId}/astrometry/footprint", GetFootprintAsync)
                .WithSummary("Reports the sky footprint (corner RA/Dec) of a staged image.");
        }
    }

    private static async Task<IResult> GetFootprintAsync(string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
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

        var (width, height) = dataset.Image.Resolve2DDimensions();

        var cornersResult = ResolveCorners(wcsResult.Value, width, height);

        return cornersResult.ToApiResult(corners => Results.Ok(ImageFootprintResponse.Create(fileId, corners)));
    }

    private static Result<ImmutableList<WorldPointDto>> ResolveCorners(Wcs wcs, int width, int height)
    {
        Span<(double PixelX, double PixelY)> pixelCorners =
        [
            (0, 0),
            (width - 1, 0),
            (0, height - 1),
            (width - 1, height - 1),
        ];

        var builder = ImmutableList.CreateBuilder<WorldPointDto>();

        foreach (var (pixelX, pixelY) in pixelCorners)
        {
            var worldResult = wcs.PixelToWorld(pixelX, pixelY);

            if (worldResult.IsFailure)
            {
                return Result<ImmutableList<WorldPointDto>>.Failure(worldResult.Error);
            }

            builder.Add(WorldPointDto.Create(worldResult.Value.RightAscension, worldResult.Value.Declination));
        }

        return builder.ToImmutable();
    }
}
