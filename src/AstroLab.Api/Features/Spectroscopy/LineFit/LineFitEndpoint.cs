namespace AstroLab.Api.Features.Spectroscopy.LineFit;

/// <summary>Roadmap: fits a Gaussian profile to a spectral line over a supplied wavelength region. Not yet implemented (HTTP 501).</summary>
public static class LineFitEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapLineFitEndpoint()
        {
            group.MapPost("/{fileId}/lines/fit", FitLineAsync)
                .WithSummary("Roadmap: fits a Gaussian line profile (centre, amplitude, FWHM, integrated flux) over a supplied wavelength region. Not yet implemented (HTTP 501).");
        }
    }

    private static Task<IResult> FitLineAsync(string fileId, LineFitRequest request, CancellationToken cancellationToken)
    {
        request.Validate();

        return Task.FromResult(NotImplementedResult.Value(
            "spectroscopy.line_fit.not_implemented",
            "Spectral line fitting is not yet implemented."));
    }
}
