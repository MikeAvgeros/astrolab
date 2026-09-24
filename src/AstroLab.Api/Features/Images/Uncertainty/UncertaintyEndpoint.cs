using AstroLab.Core.Imaging;
using AstroLab.Core.Photometry;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Uncertainty;

/// <summary>Estimates propagated flux uncertainty for an aperture photometry measurement, using the CCD equation when a detector gain is available.</summary>
public static class UncertaintyEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapUncertaintyEndpoint()
        {
            group.MapPost("/{fileId}/photometry/uncertainty", EstimateUncertaintyAsync)
                .WithSummary("Estimates the propagated flux uncertainty of an aperture measurement (source shot noise, sky-background noise, and read noise when available).");
        }
    }

    private static async Task<IResult> EstimateUncertaintyAsync(
        string fileId, UncertaintyRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
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

        return uncertaintyResult.ToApiResult(fluxUncertainty =>
            Results.Ok(UncertaintyResponse.Create(fileId, measurement.NetFlux, fluxUncertainty, gain)));
    }
}
