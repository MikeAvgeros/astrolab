namespace AstroLab.Api.Features.TimeSeries.PeriodSearch;

/// <summary>
/// Roadmap slice: periodicity search (e.g. Lomb-Scargle) over a light curve. Request/response
/// contract is final; the search algorithm itself is not yet implemented (see spec.md §6.5), so
/// this route always returns HTTP 501.
/// </summary>
public static class PeriodSearchEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapPeriodSearchEndpoint()
        {
            group.MapGet("/{fileId}/period-search", SearchForPeriod)
                .WithSummary("Searches a light curve for periodic signals. Not yet implemented.");
        }
    }

    private static IResult SearchForPeriod(string fileId, double minPeriod, double maxPeriod)
    {
        _ = PeriodSearchRequest.Create(minPeriod, maxPeriod);

        return NotImplementedResult.Value("timeseries.periodsearch.not_implemented", "Periodicity search is not yet implemented.");
    }
}
