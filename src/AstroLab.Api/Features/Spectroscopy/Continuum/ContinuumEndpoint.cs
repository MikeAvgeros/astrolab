namespace AstroLab.Api.Features.Spectroscopy.Continuum;

/// <summary>Roadmap: fits a continuum model (polynomial, with optional exclusion ranges and sigma-clipping) to a one-dimensional spectrum. Not yet implemented (HTTP 501).</summary>
public static class ContinuumEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapContinuumEndpoint()
        {
            group.MapPost("/{fileId}/continuum", FitContinuumAsync)
                .WithSummary("Roadmap: fits a polynomial continuum model to a one-dimensional spectrum without mutating the original spectrum. Not yet implemented (HTTP 501).");
        }
    }

    private static Task<IResult> FitContinuumAsync(string fileId, ContinuumRequest request, CancellationToken cancellationToken)
    {
        request.Validate();

        return Task.FromResult(NotImplementedResult.Value(
            "spectroscopy.continuum_fit.not_implemented",
            "Continuum fitting is not yet implemented."));
    }
}
