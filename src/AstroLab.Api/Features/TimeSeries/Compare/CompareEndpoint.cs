using System.Collections.Immutable;
using AstroLab.Core.TimeSeries;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.TimeSeries.Compare;

/// <summary>
/// Compares a staged light curve against one or more other staged light curves that are
/// time-aligned sample-for-sample with it (e.g. simultaneous target/comparison-star photometry
/// from the same exposures — see <see cref="LightCurveComparer"/>), reporting their correlation,
/// mean magnitude offset, flux and variability ratios, and (where the sampling allows it) a
/// comparison of their best-fit periods.
/// </summary>
public static class CompareEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapCompareEndpoint()
        {
            group.MapPost("/{fileId}/compare", CompareLightCurvesAsync)
                .WithSummary("Compares a staged light curve against one or more other staged light curves.");
        }
    }

    private static async Task<IResult> CompareLightCurvesAsync(
        string fileId, LightCurveCompareRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        request.Validate();

        var primaryResult = await datasetReader.LoadLightCurveAsync(fileId, cancellationToken);

        if (primaryResult.IsFailure)
        {
            return primaryResult.Error.ToProblem();
        }

        var primary = primaryResult.Value;

        var primaryBestPeriod = TryFindBestPeriod(primary.Time, primary.Flux);

        var entries = ImmutableList.CreateBuilder<LightCurveComparisonEntry>();

        foreach (var comparisonFileId in request.ComparisonFileIds)
        {
            var comparisonResult = await datasetReader.LoadLightCurveAsync(comparisonFileId, cancellationToken);

            if (comparisonResult.IsFailure)
            {
                return comparisonResult.Error.ToProblem();
            }

            var comparison = comparisonResult.Value;

            var compareResult = LightCurveComparer.Compare(primary.Time, primary.Flux, comparison.Time, comparison.Flux);

            if (compareResult.IsFailure)
            {
                return compareResult.Error.ToProblem();
            }

            var comparisonBestPeriod = TryFindBestPeriod(comparison.Time, comparison.Flux);

            var compare = compareResult.Value;

            entries.Add(LightCurveComparisonEntry.Create(
                comparisonFileId,
                compare.CorrelationCoefficient,
                compare.MeanMagnitudeDifference,
                compare.FluxRatio,
                compare.VariabilityRatio,
                primaryBestPeriod,
                comparisonBestPeriod));
        }

        return Results.Ok(LightCurveCompareResponse.Create(fileId, entries.ToImmutable()));
    }

    private static double? TryFindBestPeriod(ReadOnlySpan<double> time, ReadOnlySpan<double> flux)
    {
        var rangeResult = LombScarglePeriodogram.SuggestPeriodRange(time);

        if (rangeResult.IsFailure)
        {
            return null;
        }

        var detrendResult = LightCurveDetrender.Detrend(time, flux, "linear");

        if (detrendResult.IsFailure)
        {
            return null;
        }

        var searchResult = LombScarglePeriodogram.Search(
            time, detrendResult.Value, rangeResult.Value.MinPeriod, rangeResult.Value.MaxPeriod);

        return searchResult.IsSuccess ? searchResult.Value.BestPeriod : null;
    }
}
