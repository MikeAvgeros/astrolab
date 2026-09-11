namespace AstroLab.Api.Features.Spectroscopy.Snr;

/// <summary>Roadmap: reports a representative spectral signal-to-noise ratio. Not yet implemented (HTTP 501).</summary>
public static class SnrEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapSnrEndpoint()
        {
            group.MapGet("/{fileId}/snr", GetSnrAsync)
                .WithSummary("Roadmap: reports overall and per-sample spectral signal-to-noise ratio. Not yet implemented (HTTP 501).");
        }
    }

    private static Task<IResult> GetSnrAsync(string fileId, CancellationToken cancellationToken) =>
        Task.FromResult(NotImplementedResult.Value(
            "spectroscopy.snr.not_implemented",
            "Spectral signal-to-noise calculation is not yet implemented."));
}
