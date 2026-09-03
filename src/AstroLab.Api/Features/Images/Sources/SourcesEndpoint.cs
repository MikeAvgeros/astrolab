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
        string fileId, [AsParameters] SourceDetectionRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
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
            .Select(source => DetectedSourceDto.FromDetectedSource(source, wcs))
            .ToImmutableList();

        return Results.Ok(SourceDetectionResponse.Create(fileId, sourceDtos));
    }
}
