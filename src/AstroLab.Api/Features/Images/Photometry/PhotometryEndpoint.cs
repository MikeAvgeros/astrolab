using AstroLab.Core.Imaging;
using AstroLab.Core.Photometry;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Photometry;

/// <summary>Aperture photometry: background-subtracted flux measurement at a pixel position.</summary>
public static class PhotometryEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapPhotometryEndpoint()
        {
            group.MapPost("/{fileId}/photometry/aperture", MeasureApertureAsync)
                .WithSummary("Measures background-subtracted aperture flux at a pixel position in the first image-bearing HDU.");
        }
    }

    private static async Task<IResult> MeasureApertureAsync(
        string fileId,
        AperturePhotometryRequest request,
        FitsDatasetReader datasetReader,
        CancellationToken cancellationToken)
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
            dataset.Pixels,
            width,
            height,
            request.CenterX,
            request.CenterY,
            request.ApertureRadius,
            request.AnnulusInnerRadius,
            request.AnnulusOuterRadius,
            request.BackgroundMethod);

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

        var uncertaintyResult = PhotometricUncertainty.EstimateFluxUncertainty(measurement.NetFlux, measurement.ApertureArea, skySigma);

        if (uncertaintyResult.IsFailure)
        {
            return uncertaintyResult.Error.ToProblem();
        }

        var fluxUncertainty = uncertaintyResult.Value;

        var snrResult = PhotometricUncertainty.ComputeSignalToNoiseRatio(measurement.NetFlux, fluxUncertainty);

        return snrResult.ToApiResult(snr => Results.Ok(AperturePhotometryResponse.Create(
            fileId,
            measurement.RawFlux,
            measurement.ApertureArea,
            measurement.BackgroundPerPixel,
            measurement.NetFlux,
            fluxUncertainty,
            snr)));
    }
}
