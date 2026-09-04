using AstroLab.Api.Features.TimeSeries.Compare;
using AstroLab.Api.Features.TimeSeries.Detrend;
using AstroLab.Api.Features.TimeSeries.LightCurve;
using AstroLab.Api.Features.TimeSeries.PeriodSearch;
using AstroLab.Api.Features.TimeSeries.Transit;

namespace AstroLab.Api.Features.TimeSeries;

public static class TimeSeriesEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapTimeSeriesEndpoints()
        {
            var group = app.MapGroup("/api/timeseries").WithTags("TimeSeries");

            group.MapLightCurveEndpoint();

            group.MapDetrendEndpoint();

            group.MapPeriodSearchEndpoint();

            group.MapTransitEndpoint();

            group.MapCompareEndpoint();
        }
    }
}
