namespace AstroLab.Api.Features.TimeSeries.Transit;

/// <summary>
/// Roadmap slice: periodic transit (brightness-dip) search over a light curve, e.g. for exoplanet
/// detection. Request/response contract is final; the search algorithm itself is not yet
/// implemented (see spec.md §6.5), so this route always returns HTTP 501.
/// </summary>
public static class TransitEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapTransitEndpoint()
        {
            group.MapGet("/{fileId}/transit", SearchForTransits)
                .WithSummary("Searches a light curve for periodic transit (brightness-dip) signals. Not yet implemented.");
        }
    }

    private static IResult SearchForTransits(string fileId, double minPeriod, double maxPeriod, double minTransitDepth)
    {
        _ = TransitRequest.Create(minPeriod, maxPeriod, minTransitDepth);

        return NotImplementedResult.Value("timeseries.transit.not_implemented", "Transit search is not yet implemented.");
    }
}
