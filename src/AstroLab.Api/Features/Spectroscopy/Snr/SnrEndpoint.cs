using AstroLab.Core.Spectroscopy;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Spectroscopy.Snr;

/// <summary>Reports a representative spectral signal-to-noise ratio.</summary>
public static class SnrEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapSnrEndpoint()
        {
            group.MapGet("/{fileId}/snr", GetSnrAsync)
                .WithSummary("Reports overall and per-sample spectral signal-to-noise ratio.");
        }
    }

    private static async Task<IResult> GetSnrAsync(string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var datasetResult = await datasetReader.LoadSpectrumImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var spectrumResult = SpectrumFrameExtraction.ExtractFullFrame(dataset);

        if (spectrumResult.IsFailure)
        {
            return spectrumResult.Error.ToProblem();
        }

        var snrResult = SpectrumSignalToNoiseEstimator.Estimate(spectrumResult.Value);

        return snrResult.ToApiResult(snr => Results.Ok(SnrResponse.Create(fileId, snr.OverallSnr, [.. snr.PerSampleSnr])));
    }
}
