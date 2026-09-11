namespace AstroLab.Api.Features.TimeSeries.Variability;

/// <summary>Roadmap: calculates variability statistics (mean, median, standard deviation, amplitude, RMS, MAD) for a time series. Not yet implemented (HTTP 501).</summary>
public static class VariabilityEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapVariabilityEndpoint()
        {
            group.MapGet("/{fileId}/variability", GetVariabilityAsync)
                .WithSummary("Roadmap: reports time-series variability statistics for a light curve. Not yet implemented (HTTP 501).");
        }
    }

    private static Task<IResult> GetVariabilityAsync(string fileId, CancellationToken cancellationToken) =>
        Task.FromResult(NotImplementedResult.Value(
            "timeseries.variability.not_implemented",
            "Variability statistics calculation is not yet implemented."));
}
