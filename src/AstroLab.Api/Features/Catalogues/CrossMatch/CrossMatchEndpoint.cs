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

    private const int PerSourceSearchMaxResults = 50;

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

        var fieldResult = ComputeSearchField(sourcePositions, request.RadiusArcsec);

        if (fieldResult.IsFailure)
        {
            return fieldResult.Error.ToProblem();
        }

        var candidatesResult = await ResolveCandidatesAsync(
            request.CatalogueIds, fieldResult.Value, sourcePositions, request.RadiusArcsec, catalogueClient, cancellationToken);

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

    private static Result<(double CenterRightAscension, double CenterDeclination, double RadiusArcsec)> ComputeSearchField(
        IReadOnlyList<(int SourceId, double RightAscension, double Declination)> sourcePositions, double matchRadiusArcsec)
    {
        var centroidResult = SphericalCentroid.Compute(
            sourcePositions.Select(source => (source.RightAscension, source.Declination)).ToArray());

        if (centroidResult.IsFailure)
        {
            return Result<(double, double, double)>.Failure(centroidResult.Error);
        }

        var (centerRightAscension, centerDeclination) = centroidResult.Value;

        var maxSeparationArcsec = 0.0;

        foreach (var source in sourcePositions)
        {
            var separationResult = AngularSeparation.ComputeArcseconds(
                centerRightAscension, centerDeclination, source.RightAscension, source.Declination);

            if (separationResult.IsFailure)
            {
                return Result<(double, double, double)>.Failure(separationResult.Error);
            }

            if (separationResult.Value > maxSeparationArcsec)
            {
                maxSeparationArcsec = separationResult.Value;
            }
        }

        return (centerRightAscension, centerDeclination, maxSeparationArcsec + matchRadiusArcsec);
    }

    private static async Task<Result<ImmutableList<CatalogueMatchCandidate>>> ResolveCandidatesAsync(
        IReadOnlyList<string> catalogueIds,
        (double CenterRightAscension, double CenterDeclination, double RadiusArcsec) field,
        IReadOnlyList<(int SourceId, double RightAscension, double Declination)> sourcePositions,
        double matchRadiusArcsec,
        ICatalogueClient catalogueClient,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableList.CreateBuilder<CatalogueMatchCandidate>();

        foreach (var catalogueId in catalogueIds)
        {
            var fieldQuery = CatalogueConeSearchQuery.Create(
                catalogueId, field.CenterRightAscension, field.CenterDeclination, field.RadiusArcsec, CandidateSearchMaxResults);

            var fieldResult = await catalogueClient.ConeSearchAsync(fieldQuery, cancellationToken);

            if (fieldResult.IsFailure)
            {
                return Result<ImmutableList<CatalogueMatchCandidate>>.Failure(fieldResult.Error);
            }

            // A result at the row cap may be truncated (VizieR returns the rows nearest the field centre),
            // which would silently drop candidates for sources near the field edge; fall back to one small
            // cone per source, which is complete for the requested match radius.
            var recordsResult = fieldResult.Value.Count < CandidateSearchMaxResults
                ? fieldResult
                : await SearchAroundEachSourceAsync(catalogueId, sourcePositions, matchRadiusArcsec, catalogueClient, cancellationToken);

            if (recordsResult.IsFailure)
            {
                return Result<ImmutableList<CatalogueMatchCandidate>>.Failure(recordsResult.Error);
            }

            foreach (var record in recordsResult.Value)
            {
                builder.Add(CatalogueMatchCandidate.Create(
                    catalogueId, record.Identifier, record.RightAscension, record.Declination, record.Magnitude));
            }
        }

        return builder.ToImmutable();
    }

    private static async Task<Result<IReadOnlyList<CatalogueRecord>>> SearchAroundEachSourceAsync(
        string catalogueId,
        IReadOnlyList<(int SourceId, double RightAscension, double Declination)> sourcePositions,
        double matchRadiusArcsec,
        ICatalogueClient catalogueClient,
        CancellationToken cancellationToken)
    {
        var records = new List<CatalogueRecord>();

        var seenIdentifiers = new HashSet<string>(StringComparer.Ordinal);

        foreach (var source in sourcePositions)
        {
            var query = CatalogueConeSearchQuery.Create(
                catalogueId, source.RightAscension, source.Declination, matchRadiusArcsec, PerSourceSearchMaxResults);

            var result = await catalogueClient.ConeSearchAsync(query, cancellationToken);

            if (result.IsFailure)
            {
                return result;
            }

            foreach (var record in result.Value)
            {
                if (seenIdentifiers.Add(record.Identifier))
                {
                    records.Add(record);
                }
            }
        }

        return records;
    }
}
