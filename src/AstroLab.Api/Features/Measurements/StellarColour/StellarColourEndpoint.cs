using AstroLab.Core.Fits;
using AstroLab.Core.Imaging;
using AstroLab.Core.Photometry;
using AstroLab.Core.Result;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Measurements.StellarColour;

/// <summary>
/// Measures a star's brightness and colour index from aperture photometry at the same pixel
/// position across two staged images taken in different bands: the primary image (<c>fileId</c>)
/// and a comparison image (<see cref="StellarColourRequest.ComparisonFileId"/>). Each band's flux
/// is background-subtracted using the local sky level estimated in the requested annulus, since a
/// sky-inclusive aperture sum would drive the colour index towards zero on a bright sky. Both
/// instrumental magnitudes share the same zero point, so the colour index (primary minus comparison magnitude)
/// is meaningful even though neither magnitude is independently calibrated.
/// </summary>
public static class StellarColourEndpoint
{
    private const string GainKeyword = "GAIN";

    extension(IEndpointRouteBuilder group)
    {
        public void MapStellarColourEndpoint()
        {
            group.MapPost("/{fileId}/stellar-colour", MeasureStellarColourAsync)
                .WithSummary("Measures a star's brightness and colour index across two staged images in different bands.");
        }
    }

    private static async Task<IResult> MeasureStellarColourAsync(
        string fileId, StellarColourRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        request.Validate();

        var primaryDatasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (primaryDatasetResult.IsFailure)
        {
            return primaryDatasetResult.Error.ToProblem();
        }

        using var primaryDataset = primaryDatasetResult.Value;

        var secondaryDatasetResult = await datasetReader.LoadImageAsync(request.ComparisonFileId, cancellationToken);

        if (secondaryDatasetResult.IsFailure)
        {
            return secondaryDatasetResult.Error.ToProblem();
        }

        using var secondaryDataset = secondaryDatasetResult.Value;

        var primaryMagnitudeResult = MeasureInstrumentalMagnitude(primaryDataset, request);

        if (primaryMagnitudeResult.IsFailure)
        {
            return primaryMagnitudeResult.Error.ToProblem();
        }

        var secondaryMagnitudeResult = MeasureInstrumentalMagnitude(secondaryDataset, request);

        if (secondaryMagnitudeResult.IsFailure)
        {
            return secondaryMagnitudeResult.Error.ToProblem();
        }

        var (primaryMagnitude, primaryMagnitudeUncertainty) = primaryMagnitudeResult.Value;

        var (secondaryMagnitude, secondaryMagnitudeUncertainty) = secondaryMagnitudeResult.Value;

        var (colourIndex, colourIndexUncertainty) = InstrumentalPhotometry.ComputeDifferentialMagnitude(
            primaryMagnitude, primaryMagnitudeUncertainty, secondaryMagnitude, secondaryMagnitudeUncertainty);

        return Results.Ok(StellarColourResponse.Create(
            fileId, request.ComparisonFileId,
            primaryMagnitude, primaryMagnitudeUncertainty,
            secondaryMagnitude, secondaryMagnitudeUncertainty,
            colourIndex, colourIndexUncertainty,
            InstrumentalPhotometry.DefaultZeroPoint));
    }

    private static Result<(double Magnitude, double MagnitudeUncertainty)> MeasureInstrumentalMagnitude(
        FitsDataset dataset, StellarColourRequest request)
    {
        var (width, height) = dataset.Image.Resolve2DDimensions();

        var measurementResult = ApertureEngine.MeasureNetFlux(
            dataset.Pixels, width, height, request.CenterX, request.CenterY,
            request.ApertureRadius, request.AnnulusInnerRadius, request.AnnulusOuterRadius, request.BackgroundMethod);

        if (measurementResult.IsFailure)
        {
            return Result<(double Magnitude, double MagnitudeUncertainty)>.Failure(measurementResult.Error);
        }

        var measurement = measurementResult.Value;

        var statsResult = ImageStatistics.Compute(dataset.Pixels);

        if (statsResult.IsFailure)
        {
            return Result<(double Magnitude, double MagnitudeUncertainty)>.Failure(statsResult.Error);
        }

        var skySigma = ImageStatistics.ComputeSkyBackground(dataset.Pixels, statsResult.Value).SkySigma;

        var gain = ResolveHeaderGain(dataset.Hdu.Header);

        var fluxUncertaintyResult = PhotometricUncertainty.EstimateFluxUncertainty(
            measurement.NetFlux, measurement.ApertureArea, skySigma, gain);

        if (fluxUncertaintyResult.IsFailure)
        {
            return Result<(double Magnitude, double MagnitudeUncertainty)>.Failure(fluxUncertaintyResult.Error);
        }

        return InstrumentalPhotometry.ComputeMagnitude(
            measurement.NetFlux, fluxUncertaintyResult.Value, InstrumentalPhotometry.DefaultZeroPoint);
    }

    private static double? ResolveHeaderGain(FitsHeader header)
    {
        var gainResult = header.GetReal(GainKeyword);

        return gainResult.IsSuccess ? gainResult.Value : null;
    }
}
