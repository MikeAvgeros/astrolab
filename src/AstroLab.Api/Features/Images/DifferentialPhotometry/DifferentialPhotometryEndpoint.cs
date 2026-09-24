using AstroLab.Core.Imaging;
using AstroLab.Core.Photometry;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.DifferentialPhotometry;

/// <summary>Differential photometry between a target aperture and a comparison aperture in the same staged image.</summary>
public static class DifferentialPhotometryEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapDifferentialPhotometryEndpoint()
        {
            group.MapPost("/{fileId}/photometry/differential", MeasureDifferentialAsync)
                .WithSummary("Measures differential photometry between a target and comparison aperture.");
        }
    }

    private static async Task<IResult> MeasureDifferentialAsync(
        string fileId, DifferentialPhotometryRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        request.Validate();

        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var (width, height) = dataset.Image.Resolve2DDimensions();

        var targetResult = ApertureEngine.MeasureNetFlux(
            dataset.Pixels, width, height, request.TargetCenterX, request.TargetCenterY,
            request.ApertureRadius, request.AnnulusInnerRadius, request.AnnulusOuterRadius);

        if (targetResult.IsFailure)
        {
            return targetResult.Error.ToProblem();
        }

        var comparisonResult = ApertureEngine.MeasureNetFlux(
            dataset.Pixels, width, height, request.ComparisonCenterX, request.ComparisonCenterY,
            request.ApertureRadius, request.AnnulusInnerRadius, request.AnnulusOuterRadius);

        if (comparisonResult.IsFailure)
        {
            return comparisonResult.Error.ToProblem();
        }

        var statsResult = ImageStatistics.Compute(dataset.Pixels);

        if (statsResult.IsFailure)
        {
            return statsResult.Error.ToProblem();
        }

        var skySigma = ImageStatistics.ComputeSkyBackground(dataset.Pixels, statsResult.Value).SkySigma;

        var target = targetResult.Value;

        var comparison = comparisonResult.Value;

        var gain = PhotometricUncertainty.ReadDetectorGain(dataset.Hdu.Header);

        var targetUncertaintyResult = PhotometricUncertainty.EstimateFluxUncertainty(target, skySigma, gain);

        if (targetUncertaintyResult.IsFailure)
        {
            return targetUncertaintyResult.Error.ToProblem();
        }

        var comparisonUncertaintyResult = PhotometricUncertainty.EstimateFluxUncertainty(comparison, skySigma, gain);

        if (comparisonUncertaintyResult.IsFailure)
        {
            return comparisonUncertaintyResult.Error.ToProblem();
        }

        var targetUncertainty = targetUncertaintyResult.Value;

        var comparisonUncertainty = comparisonUncertaintyResult.Value;

        var targetMagnitudeResult = InstrumentalPhotometry.ComputeMagnitude(
            target.NetFlux, targetUncertainty, InstrumentalPhotometry.DefaultZeroPoint);

        if (targetMagnitudeResult.IsFailure)
        {
            return targetMagnitudeResult.Error.ToProblem();
        }

        var comparisonMagnitudeResult = InstrumentalPhotometry.ComputeMagnitude(
            comparison.NetFlux, comparisonUncertainty, InstrumentalPhotometry.DefaultZeroPoint);

        if (comparisonMagnitudeResult.IsFailure)
        {
            return comparisonMagnitudeResult.Error.ToProblem();
        }

        var targetMagnitude = targetMagnitudeResult.Value;

        var comparisonMagnitude = comparisonMagnitudeResult.Value;

        var (differentialMagnitude, uncertainty) = InstrumentalPhotometry.ComputeDifferentialMagnitude(
            targetMagnitude.Magnitude, targetMagnitude.MagnitudeUncertainty, comparisonMagnitude.Magnitude, comparisonMagnitude.MagnitudeUncertainty);

        var targetSnr = PhotometricUncertainty.ComputeSignalToNoiseRatio(
            target.NetFlux, targetUncertainty);

        var comparisonSnr = PhotometricUncertainty.ComputeSignalToNoiseRatio(
            comparison.NetFlux, comparisonUncertainty);

        return Results.Ok(DifferentialPhotometryResponse.Create(
            fileId, targetMagnitude.Magnitude, comparisonMagnitude.Magnitude, differentialMagnitude, uncertainty,
            targetSnr.IsSuccess ? targetSnr.Value : null, comparisonSnr.IsSuccess ? comparisonSnr.Value : null));
    }
}
