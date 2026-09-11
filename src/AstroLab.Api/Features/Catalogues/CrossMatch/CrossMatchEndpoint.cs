using System.Collections.Immutable;
using AstroLab.Core.Astrometry;
using AstroLab.Core.Catalogues;
using AstroLab.Core.Result;
using AstroLab.Core.Sources;
using AstroLab.Infrastructure.Catalogues;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Catalogues.CrossMatch;

/// <summary>
/// Cross-matches sources detected in a staged image against one or more external catalogues:
/// detects sources, resolves each to RA/Dec via the file's WCS, cone-searches every requested
/// catalogue over the sky field the detected sources span, then pairs each source with its
/// nearest catalogue candidate within the requested radius.
/// </summary>
public static class CrossMatchEndpoint
{
    /// <summary>Per-catalogue candidate cap for the single field-covering cone search backing a cross-match request.</summary>
    private const int CandidateSearchMaxResults = 2000;

    extension(IEndpointRouteBuilder group)
    {
        public void MapCrossMatchEndpoint()
        {
            group.MapPost("/cross-match", CrossMatchSourcesAsync)
                .WithSummary("Cross-matches detected sources in a staged image against one or more external catalogues.");
        }
    }

    private static async Task<IResult> CrossMatchSourcesAsync(
        CrossMatchRequest request,
        FitsDatasetReader datasetReader,
        ICatalogueClient catalogueClient,
        CancellationToken cancellationToken)
    {
        request.Validate();

        var datasetResult = await datasetReader.LoadImageAsync(request.FileId, cancellationToken);

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

        var detectionResult = SourceDetector.Detect(dataset.Pixels, width, height);

        if (detectionResult.IsFailure)
        {
            return detectionResult.Error.ToProblem();
        }

        var sourcePositionsResult = ResolveSourcePositions(wcsResult.Value, detectionResult.Value);

        if (sourcePositionsResult.IsFailure)
        {
            return sourcePositionsResult.Error.ToProblem();
        }

        var sourcePositions = sourcePositionsResult.Value;

        if (sourcePositions.Count == 0)
        {
            return Results.Ok(CrossMatchResponse.Create(request.FileId, []));
        }

        var field = ComputeSearchField(sourcePositions, request.RadiusArcsec);

        var candidatesResult = await ResolveCandidatesAsync(request.CatalogueIds, field, catalogueClient, cancellationToken);

        if (candidatesResult.IsFailure)
        {
            return candidatesResult.Error.ToProblem();
        }

        var matchResult = CatalogueCrossMatcher.Match(sourcePositions, candidatesResult.Value, request.RadiusArcsec);

        return matchResult.ToApiResult(matches => Results.Ok(CrossMatchResponse.Create(request.FileId, matches)));
    }

    private static Result<ImmutableList<(int SourceId, double RightAscension, double Declination)>> ResolveSourcePositions(
        Wcs wcs, ImmutableArray<DetectedSource> sources)
    {
        var builder = ImmutableList.CreateBuilder<(int SourceId, double RightAscension, double Declination)>();

        foreach (var source in sources)
        {
            var worldResult = wcs.PixelToWorld(source.PixelX, source.PixelY);

            if (worldResult.IsFailure)
            {
                return Result<ImmutableList<(int SourceId, double RightAscension, double Declination)>>.Failure(worldResult.Error);
            }

            builder.Add((source.Id, worldResult.Value.RightAscension, worldResult.Value.Declination));
        }

        return builder.ToImmutable();
    }

    private static (double CenterRightAscension, double CenterDeclination, double RadiusArcsec) ComputeSearchField(
        IReadOnlyList<(int SourceId, double RightAscension, double Declination)> sourcePositions, double matchRadiusArcsec)
    {
        var centerRightAscension = sourcePositions.Average(source => source.RightAscension);

        var centerDeclination = sourcePositions.Average(source => source.Declination);

        var maxSeparationArcsec = 0.0;

        foreach (var source in sourcePositions)
        {
            var separationResult = AngularSeparation.ComputeArcseconds(
                centerRightAscension, centerDeclination, source.RightAscension, source.Declination);

            if (separationResult.IsSuccess && separationResult.Value > maxSeparationArcsec)
            {
                maxSeparationArcsec = separationResult.Value;
            }
        }

        return (centerRightAscension, centerDeclination, maxSeparationArcsec + matchRadiusArcsec);
    }

    private static async Task<Result<ImmutableList<CatalogueMatchCandidate>>> ResolveCandidatesAsync(
        IReadOnlyList<string> catalogueIds,
        (double CenterRightAscension, double CenterDeclination, double RadiusArcsec) field,
        ICatalogueClient catalogueClient,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableList.CreateBuilder<CatalogueMatchCandidate>();

        foreach (var catalogueId in catalogueIds)
        {
            var query = CatalogueConeSearchQuery.Create(
                catalogueId, field.CenterRightAscension, field.CenterDeclination, field.RadiusArcsec, CandidateSearchMaxResults);

            var result = await catalogueClient.ConeSearchAsync(query, cancellationToken);

            if (result.IsFailure)
            {
                return Result<ImmutableList<CatalogueMatchCandidate>>.Failure(result.Error);
            }

            foreach (var record in result.Value)
            {
                builder.Add(CatalogueMatchCandidate.Create(
                    catalogueId, record.Identifier, record.RightAscension, record.Declination, record.Magnitude));
            }
        }

        return builder.ToImmutable();
    }
}
