namespace AstroLab.Api.Features.TimeSeries.PhaseFold;

/// <summary>Roadmap: folds a time series around a supplied period and reference epoch. Not yet implemented (HTTP 501).</summary>
public static class PhaseFoldEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapPhaseFoldEndpoint()
        {
            group.MapPost("/{fileId}/phase-fold", FoldPhaseAsync)
                .WithSummary("Roadmap: folds a light curve around a supplied period and reference epoch, preserving uncertainty and original time where available. Not yet implemented (HTTP 501).");
        }
    }

    private static Task<IResult> FoldPhaseAsync(string fileId, PhaseFoldRequest request, CancellationToken cancellationToken)
    {
        request.Validate();

        return Task.FromResult(NotImplementedResult.Value(
            "timeseries.phase_fold.not_implemented",
            "Phase folding is not yet implemented."));
    }
}
