namespace AstroLab.Api.Features.Spectroscopy.Compare;

/// <summary>
/// Roadmap slice: comparing two staged spectra via cross-correlation to measure their relative
/// velocity shift. Request/response contract is final; the comparison algorithm itself is not yet
/// implemented (see spec.md §6.5), so this route always returns HTTP 501.
/// </summary>
public static class CompareEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapCompareEndpoint()
        {
            group.MapPost("/{fileId}/compare", CompareSpectra)
                .WithSummary("Compares two staged spectra via cross-correlation. Not yet implemented.");
        }
    }

    private static IResult CompareSpectra(string fileId, SpectrumCompareRequest request)
    {
        request.Validate();

        return NotImplementedResult.Value("spectroscopy.compare.not_implemented", "Spectrum comparison is not yet implemented.");
    }
}
