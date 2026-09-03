namespace AstroLab.Api.Features.Spectroscopy.Lines;

/// <summary>
/// Roadmap slice: spectral line detection over an extracted 1D spectrum. Request/response
/// contract is final; the line-detection algorithm itself is not yet implemented (see spec.md
/// §4.1), so this route always returns HTTP 501.
/// </summary>
public static class LinesEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapLinesEndpoint()
        {
            group.MapGet("/{fileId}/lines", DetectLines)
                .WithSummary("Detects spectral lines in an extracted 1D spectrum. Not yet implemented.");
        }
    }

    private static IResult DetectLines(string fileId, double? significanceThreshold = null)
    {
        _ = LineDetectionRequest.Create(significanceThreshold);

        return NotImplementedResult.Value("spectroscopy.lines.not_implemented", "Spectral line detection is not yet implemented.");
    }
}
