using AstroLab.Core.TimeSeries;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.TimeSeries.Variability;

/// <summary>Calculates variability statistics (mean, median, standard deviation, amplitude, RMS, MAD) for a staged light curve.</summary>
public static class VariabilityEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapVariabilityEndpoint()
        {
            group.MapGet("/{fileId}/variability", GetVariabilityAsync)
                .WithSummary("Reports time-series variability statistics for a staged light curve.");
        }
    }

    private static async Task<IResult> GetVariabilityAsync(string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var lightCurveResult = await datasetReader.LoadLightCurveAsync(fileId, cancellationToken);

        if (lightCurveResult.IsFailure)
        {
            return lightCurveResult.Error.ToProblem();
        }

        var data = lightCurveResult.Value;

        var analyzeResult = LightCurveVariabilityAnalyzer.Analyze(data.Flux);

        return analyzeResult.ToApiResult(stats => Results.Ok(VariabilityResponse.Create(
            fileId, stats.Mean, stats.Median, stats.StandardDeviation, stats.Amplitude, stats.Rms, stats.MedianAbsoluteDeviation)));
    }
}
