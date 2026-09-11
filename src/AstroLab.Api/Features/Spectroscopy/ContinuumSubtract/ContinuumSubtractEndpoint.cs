namespace AstroLab.Api.Features.Spectroscopy.ContinuumSubtract;

/// <summary>Roadmap: subtracts a fitted continuum model from a one-dimensional spectrum. Not yet implemented (HTTP 501).</summary>
public static class ContinuumSubtractEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapContinuumSubtractEndpoint()
        {
            group.MapPost("/{fileId}/continuum/subtract", SubtractContinuumAsync)
                .WithSummary("Roadmap: subtracts the fitted continuum from a spectrum, preserving wavelength and uncertainty. Not yet implemented (HTTP 501).");
        }
    }

    private static Task<IResult> SubtractContinuumAsync(string fileId, ContinuumSubtractRequest request, CancellationToken cancellationToken)
    {
        request.Validate();

        return Task.FromResult(NotImplementedResult.Value(
            "spectroscopy.continuum_subtract.not_implemented",
            "Continuum subtraction is not yet implemented."));
    }
}
