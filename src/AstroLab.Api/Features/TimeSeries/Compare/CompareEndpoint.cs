namespace AstroLab.Api.Features.TimeSeries.Compare;

/// <summary>
/// Roadmap slice: comparing two staged light curves (from different dates or instruments) via
/// their correlation and mean magnitude offset. Request/response contract is final; the
/// comparison algorithm itself is not yet implemented (see spec.md §6.5), so this route always
/// returns HTTP 501.
/// </summary>
public static class CompareEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapCompareEndpoint()
        {
            group.MapPost("/{fileId}/compare", CompareLightCurves)
                .WithSummary("Compares two staged light curves from different dates or instruments. Not yet implemented.");
        }
    }

    private static IResult CompareLightCurves(string fileId, LightCurveCompareRequest request)
    {
        request.Validate();

        return NotImplementedResult.Value("timeseries.compare.not_implemented", "Light curve comparison is not yet implemented.");
    }
}
