using AstroLab.Core.Imaging;
using AstroLab.Core.Photometry;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Snr;

/// <summary>Computes the signal-to-noise ratio of an aperture photometry measurement.</summary>
public static class SnrEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapSnrEndpoint()
        {
            group.MapPost("/{fileId}/photometry/snr", ComputeSnrAsync)
                .WithSummary("Measures aperture flux and its propagated uncertainty, then reports the resulting signal-to-noise ratio.");
        }
    }

    private static async Task<IResult> ComputeSnrAsync(
        string fileId, SnrRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        request.Validate();

        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var (width, height) = dataset.Image.Resolve2DDimensions();

        var measurementResult = ApertureEngine.MeasureNetFlux(
            dataset.Pixels, width, height, request.CenterX, request.CenterY,
            request.ApertureRadius, request.AnnulusInnerRadius, request.AnnulusOuterRadius, request.BackgroundMethod);

        if (measurementResult.IsFailure)
        {
            return measurementResult.Error.ToProblem();
        }

        var measurement = measurementResult.Value;

        var statsResult = ImageStatistics.Compute(dataset.Pixels);

        if (statsResult.IsFailure)
        {
            return statsResult.Error.ToProblem();
        }

        var skySigma = ImageStatistics.ComputeSkyBackground(dataset.Pixels, statsResult.Value).SkySigma;

        var gain = request.DetectorGain ?? PhotometricUncertainty.ReadDetectorGain(dataset.Hdu.Header);

        var uncertaintyResult = PhotometricUncertainty.EstimateFluxUncertainty(
            measurement, skySigma, gain, request.ReadNoiseElectrons);

        if (uncertaintyResult.IsFailure)
        {
            return uncertaintyResult.Error.ToProblem();
        }

        var fluxUncertainty = uncertaintyResult.Value;

        var snrResult = PhotometricUncertainty.ComputeSignalToNoiseRatio(measurement.NetFlux, fluxUncertainty);

        return snrResult.ToApiResult(snr => Results.Ok(SnrResponse.Create(fileId, measurement.NetFlux, fluxUncertainty, snr)));
    }
}
