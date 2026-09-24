using AstroLab.Core.Astrometry;
using AstroLab.Core.Fits;
using AstroLab.Core.Imaging;
using AstroLab.Core.Photometry;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Measurements.SurfaceBrightness;

/// <summary>
/// Measures surface brightness (magnitude per square arcsecond) within a circular aperture on a
/// staged image, converting the aperture's pixel area to arcsec^2 via the image's WCS pixel scale.
/// The aperture flux is background-subtracted using the local sky level estimated in the requested
/// annulus, so the result describes the source rather than the source plus sky.
/// </summary>
public static class SurfaceBrightnessEndpoint
{
    private const string GainKeyword = "GAIN";

    extension(IEndpointRouteBuilder group)
    {
        public void MapSurfaceBrightnessEndpoint()
        {
            group.MapGet("/{fileId}/surface-brightness", MeasureSurfaceBrightnessAsync)
                .WithSummary("Measures surface brightness within an aperture on a staged image.");
        }
    }

    private static async Task<IResult> MeasureSurfaceBrightnessAsync(
        string fileId,
        double centerX,
        double centerY,
        double apertureRadius,
        double annulusInnerRadius,
        double annulusOuterRadius,
        FitsDatasetReader datasetReader,
        CancellationToken cancellationToken,
        BackgroundEstimationMethod backgroundMethod = BackgroundEstimationMethod.Median)
    {
        var request = SurfaceBrightnessRequest.Create(
            centerX, centerY, apertureRadius, annulusInnerRadius, annulusOuterRadius, backgroundMethod);

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

        var gain = ResolveHeaderGain(dataset.Hdu.Header);

        var fluxUncertaintyResult = PhotometricUncertainty.EstimateFluxUncertainty(
            measurement.NetFlux, measurement.ApertureArea, skySigma, gain);

        if (fluxUncertaintyResult.IsFailure)
        {
            return fluxUncertaintyResult.Error.ToProblem();
        }

        var magnitudeResult = InstrumentalPhotometry.ComputeMagnitude(
            measurement.NetFlux, fluxUncertaintyResult.Value, InstrumentalPhotometry.DefaultZeroPoint);

        if (magnitudeResult.IsFailure)
        {
            return magnitudeResult.Error.ToProblem();
        }

        var wcsResult = Wcs.FromHeader(dataset.Hdu.Header);

        if (wcsResult.IsFailure)
        {
            return wcsResult.Error.ToProblem();
        }

        var wcs = wcsResult.Value;

        var surfaceBrightnessResult = InstrumentalPhotometry.ComputeSurfaceBrightness(
            magnitudeResult.Value.Magnitude, magnitudeResult.Value.MagnitudeUncertainty,
            measurement.ApertureArea, wcs.PixelScaleXDegrees, wcs.PixelScaleYDegrees);

        return surfaceBrightnessResult.ToApiResult(surfaceBrightness => Results.Ok(SurfaceBrightnessResponse.Create(
            fileId, surfaceBrightness.SurfaceBrightness, surfaceBrightness.SurfaceBrightnessUncertainty, InstrumentalPhotometry.DefaultZeroPoint)));
    }

    private static double? ResolveHeaderGain(FitsHeader header)
    {
        var gainResult = header.GetReal(GainKeyword);

        return gainResult.IsSuccess ? gainResult.Value : null;
    }
}
