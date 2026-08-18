using AstroLab.Api.Features.TimeSeries.Detrend;
using AstroLab.Api.Features.TimeSeries.LightCurve;
using AstroLab.Api.Features.TimeSeries.PeriodSearch;

namespace AstroLab.Api.Features.TimeSeries;

/// <summary>
/// "What can I learn from this time series?" — light curve extraction, detrending, and
/// periodicity search over a staged time-series FITS table. Scaffolded roadmap feature: every
/// leaf returns HTTP 501 pending its Core algorithm and the underlying table-reading
/// infrastructure (see spec.md §4.1).
/// </summary>
public static class TimeSeriesEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public RouteGroupBuilder MapTimeSeriesEndpoints()
        {
            var group = app.MapGroup("/api/timeseries").WithTags("TimeSeries");

            group.MapLightCurveEndpoint();
            group.MapDetrendEndpoint();
            group.MapPeriodSearchEndpoint();

            return group;
        }
    }
}
