using System.Collections.Immutable;
using AstroLab.Core.Imaging;
using AstroLab.Core.Photometry;
using AstroLab.Core.Sources;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.MultiPhotometry;

/// <summary>Aperture photometry with instrumental magnitudes and uncertainties, run over every source detected in a staged image.</summary>
public static class MultiPhotometryEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapMultiPhotometryEndpoint()
        {
            group.MapGet("/{fileId}/photometry/sources", MeasureAllSourcesAsync)
                .WithSummary("Measures aperture flux, instrumental magnitude, and uncertainty for every detected source in a staged image.");
        }
    }

    private static async Task<IResult> MeasureAllSourcesAsync(
        string fileId,
        FitsDatasetReader datasetReader,
        CancellationToken cancellationToken,
        double thresholdSigma = SourceDetector.DefaultThresholdSigma,
        int minimumArea = SourceDetector.DefaultMinimumArea,
        int maxSources = SourceDetector.DefaultMaxSources,
        double apertureRadius = MultiAperturePhotometryRequest.DefaultApertureRadius,
        double annulusInnerRadius = MultiAperturePhotometryRequest.DefaultAnnulusInnerRadius,
        double annulusOuterRadius = MultiAperturePhotometryRequest.DefaultAnnulusOuterRadius,
        double magnitudeZeroPoint = MultiAperturePhotometryRequest.DefaultMagnitudeZeroPoint)
    {
        var request = MultiAperturePhotometryRequest.Create(
            thresholdSigma, minimumArea, maxSources, apertureRadius, annulusInnerRadius, annulusOuterRadius, magnitudeZeroPoint);

        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var (width, height) = dataset.Image.Resolve2DDimensions();

        var detectionResult = SourceDetector.Detect(
            dataset.Pixels, width, height, request.ThresholdSigma, request.MinimumArea, request.MaxSources);

        if (detectionResult.IsFailure)
        {
            return detectionResult.Error.ToProblem();
        }

        var statsResult = ImageStatistics.Compute(dataset.Pixels);

        if (statsResult.IsFailure)
        {
            return statsResult.Error.ToProblem();
        }

        var skySigma = ImageStatistics.ComputeSkyBackground(dataset.Pixels, statsResult.Value).SkySigma;

        var sourceDtos = ImmutableList.CreateBuilder<SourcePhotometryDto>();

        foreach (var source in detectionResult.Value)
        {
            var measurementResult = ApertureEngine.MeasureNetFlux(
                dataset.Pixels, width, height, source.PixelX, source.PixelY,
                request.ApertureRadius, request.AnnulusInnerRadius, request.AnnulusOuterRadius);

            if (measurementResult.IsFailure)
            {
                continue;
            }

            var measurement = measurementResult.Value;

            var fluxUncertainty = InstrumentalPhotometry.EstimateFluxUncertainty(skySigma, measurement.ApertureArea);

            var magnitudeResult = InstrumentalPhotometry.ComputeMagnitude(measurement.NetFlux, fluxUncertainty, request.MagnitudeZeroPoint);

            if (magnitudeResult.IsFailure)
            {
                // Instrumental magnitude is undefined for a non-positive net flux; omit rather than fabricate one.
                continue;
            }

            sourceDtos.Add(SourcePhotometryDto.Create(
                source.Id, measurement.NetFlux, fluxUncertainty, magnitudeResult.Value.Magnitude, magnitudeResult.Value.MagnitudeUncertainty));
        }

        return Results.Ok(MultiAperturePhotometryResponse.Create(fileId, sourceDtos.ToImmutable()));
    }
}
