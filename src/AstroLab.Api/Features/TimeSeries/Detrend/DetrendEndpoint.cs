namespace AstroLab.Api.Features.TimeSeries.Detrend;

/// <summary>
/// Roadmap slice: removing long-term trends from a light curve. Request/response contract is
/// final; the detrending algorithm itself is not yet implemented (see spec.md §6.5), so this
/// route always returns HTTP 501.
/// </summary>
public static class DetrendEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapDetrendEndpoint()
        {
            group.MapPost("/{fileId}/detrend", DetrendLightCurve)
                .WithSummary("Removes long-term trends from a light curve. Not yet implemented.");
        }
    }

    private static IResult DetrendLightCurve(string fileId, DetrendRequest request)
    {
        request.Validate();

        return NotImplementedResult.Value("timeseries.detrend.not_implemented", "Light curve detrending is not yet implemented.");
    }
}
