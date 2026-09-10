using AstroLab.Core.TimeSeries;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.TimeSeries.PeriodSearch;

/// <summary>Searches a detrended light curve for periodic signals using a Lomb-Scargle periodogram.</summary>
public static class PeriodSearchEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapPeriodSearchEndpoint()
        {
            group.MapGet("/{fileId}/period-search", SearchForPeriodAsync)
                .WithSummary("Searches a light curve for periodic signals using a Lomb-Scargle periodogram.");
        }
    }

    private static async Task<IResult> SearchForPeriodAsync(
        string fileId, double minPeriod, double maxPeriod, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var request = PeriodSearchRequest.Create(minPeriod, maxPeriod);

        var lightCurveResult = await datasetReader.LoadLightCurveAsync(fileId, cancellationToken);

        if (lightCurveResult.IsFailure)
        {
            return lightCurveResult.Error.ToProblem();
        }

        var data = lightCurveResult.Value;

        var detrendResult = LightCurveDetrender.Detrend(data.Time, data.Flux, "linear");

        if (detrendResult.IsFailure)
        {
            return detrendResult.Error.ToProblem();
        }

        var searchResult = LombScarglePeriodogram.Search(data.Time, detrendResult.Value, request.MinPeriod, request.MaxPeriod);

        return searchResult.ToApiResult(search =>
            Results.Ok(PeriodSearchResponse.Create(fileId, search.BestPeriod, search.Power)));
    }
}
