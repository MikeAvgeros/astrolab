using AstroLab.Core.Astrometry;
using AstroLab.Core.Photometry;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Measurements.SurfaceBrightness;

/// <summary>
/// Measures surface brightness (magnitude per square arcsecond) within a circular aperture on a
/// staged image, converting the aperture's pixel area to arcsec^2 via the image's WCS pixel scale.
/// </summary>
public static class SurfaceBrightnessEndpoint
{
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
        FitsDatasetReader datasetReader,
        CancellationToken cancellationToken)
    {
        var request = SurfaceBrightnessRequest.Create(centerX, centerY, apertureRadius);

        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var (width, height) = dataset.Image.Resolve2DDimensions();

        var apertureResult = ApertureEngine.MeasureCircularAperture(
            dataset.Pixels, width, height, request.CenterX, request.CenterY, request.ApertureRadius);

        if (apertureResult.IsFailure)
        {
            return apertureResult.Error.ToProblem();
        }

        var magnitudeResult = InstrumentalPhotometry.ComputeMagnitude(
            apertureResult.Value.Flux, fluxUncertainty: 0.0, InstrumentalPhotometry.DefaultZeroPoint);

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
            magnitudeResult.Value.Magnitude, apertureResult.Value.Area, wcs.PixelScaleXDegrees, wcs.PixelScaleYDegrees);

        return surfaceBrightnessResult.ToApiResult(surfaceBrightness => Results.Ok(SurfaceBrightnessResponse.Create(fileId, surfaceBrightness)));
    }
}
