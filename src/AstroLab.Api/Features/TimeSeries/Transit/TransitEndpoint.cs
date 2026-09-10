using AstroLab.Core.TimeSeries;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.TimeSeries.Transit;

/// <summary>
/// Searches a light curve for periodic transit (brightness-dip) signals using a Box Least Squares
/// search, characterising a detected candidate's period, depth, duration, and epoch.
/// </summary>
public static class TransitEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapTransitEndpoint()
        {
            group.MapGet("/{fileId}/transit", SearchForTransitsAsync)
                .WithSummary("Searches a light curve for periodic transit (brightness-dip) signals using a Box Least Squares search.");
        }
    }

    private static async Task<IResult> SearchForTransitsAsync(
        string fileId, double minPeriod, double maxPeriod, double minTransitDepth, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var request = TransitRequest.Create(minPeriod, maxPeriod, minTransitDepth);

        var lightCurveResult = await datasetReader.LoadLightCurveAsync(fileId, cancellationToken);

        if (lightCurveResult.IsFailure)
        {
            return lightCurveResult.Error.ToProblem();
        }

        var data = lightCurveResult.Value;

        var searchResult = TransitSearch.Search(data.Time, data.Flux, request.MinPeriod, request.MaxPeriod, request.MinTransitDepth);

        return searchResult.ToApiResult(search => Results.Ok(
            TransitResponse.Create(fileId, search.Period, search.Depth, search.Duration, search.Epoch)));
    }
}
