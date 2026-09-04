namespace AstroLab.Api.Features.TimeSeries.LightCurve;

/// <summary>
/// Roadmap slice: extracting a light curve (flux vs. time) from a staged time-series FITS table.
/// Response contract is final; the extraction algorithm itself is not yet implemented (see
/// spec.md §6.5), so this route always returns HTTP 501.
/// </summary>
public static class LightCurveEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapLightCurveEndpoint()
        {
            group.MapGet("/{fileId}/light-curve", GetLightCurve)
                .WithSummary("Extracts a light curve (flux vs. time) from a staged time-series FITS table. Not yet implemented.");
        }
    }

    private static IResult GetLightCurve(string fileId) =>
        NotImplementedResult.Value("timeseries.lightcurve.not_implemented", "Light curve extraction is not yet implemented.");
}
