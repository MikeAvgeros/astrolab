using AstroLab.Core.TimeSeries;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.TimeSeries.Compare;

/// <summary>Compares two staged light curves (from different dates or instruments) via their correlation and mean magnitude offset.</summary>
public static class CompareEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapCompareEndpoint()
        {
            group.MapPost("/{fileId}/compare", CompareLightCurvesAsync)
                .WithSummary("Compares two staged light curves from different dates or instruments.");
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

        var comparisonResult = await datasetReader.LoadLightCurveAsync(request.ComparisonFileId, cancellationToken);

        if (comparisonResult.IsFailure)
        {
            return comparisonResult.Error.ToProblem();
        }

        var compareResult = LightCurveComparer.Compare(primaryResult.Value.Flux, comparisonResult.Value.Flux);

        return compareResult.ToApiResult(compare => Results.Ok(LightCurveCompareResponse.Create(
            fileId, request.ComparisonFileId, compare.CorrelationCoefficient, compare.MeanMagnitudeDifference)));
    }
}
