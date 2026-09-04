using System.Collections.Immutable;
using AstroLab.Core.Astrometry;
using AstroLab.Core.Sources;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Sources;

/// <summary>Detects candidate point sources in the primary image HDU of a staged FITS file.</summary>
public static class SourcesEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapSourcesEndpoint()
        {
            group.MapGet("/{fileId}/sources", DetectSourcesAsync)
                .WithSummary("Detects candidate point sources in the primary image HDU, reporting RA/Dec where the file carries a usable WCS.");
        }
    }

    private static async Task<IResult> DetectSourcesAsync(
        string fileId,
        FitsDatasetReader datasetReader,
        CancellationToken cancellationToken,
        double thresholdSigma = SourceDetector.DefaultThresholdSigma,
        int minimumArea = SourceDetector.DefaultMinimumArea,
        int maxSources = SourceDetector.DefaultMaxSources)
    {
        var request = SourceDetectionRequest.Create(thresholdSigma, minimumArea, maxSources);

        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var (width, height) = dataset.Image.Resolve2DDimensions();

        var detectionResult = SourceDetector.Detect(dataset.Pixels, width, height, request.ThresholdSigma, request.MinimumArea, request.MaxSources);

        if (detectionResult.IsFailure)
        {
            return detectionResult.Error.ToProblem();
        }

        var wcsResult = Wcs.FromHeader(dataset.Hdu.Header);

        var wcs = wcsResult.IsSuccess ? wcsResult.Value : (Wcs?)null;

        var sourceDtos = detectionResult.Value
            .Select(source =>
            {
                var (rightAscension, declination) = ResolveWorldCoordinates(wcs, source);

                return DetectedSourceDto.FromDetectedSource(source, rightAscension, declination);
            })
            .ToImmutableList();

        return Results.Ok(SourceDetectionResponse.Create(fileId, sourceDtos));
    }

    private static (double? RightAscension, double? Declination) ResolveWorldCoordinates(Wcs? wcs, DetectedSource source)
    {
        if (wcs is not { } resolvedWcs)
        {
            return (null, null);
        }

        var worldResult = resolvedWcs.PixelToWorld(source.PixelX, source.PixelY);

        return worldResult.IsSuccess
            ? (worldResult.Value.RightAscension, worldResult.Value.Declination)
            : (null, null);
    }
}
