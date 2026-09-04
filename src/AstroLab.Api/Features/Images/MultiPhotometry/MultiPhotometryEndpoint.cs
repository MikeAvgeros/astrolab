using AstroLab.Core.Sources;

namespace AstroLab.Api.Features.Images.MultiPhotometry;

/// <summary>
/// Roadmap slice: aperture photometry with instrumental magnitudes and uncertainties, run over
/// every source detected in a staged image. Request/response contract is final; the algorithm
/// itself is not yet implemented (see spec.md §4.1), so this route always returns HTTP 501.
/// </summary>
public static class MultiPhotometryEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapMultiPhotometryEndpoint()
        {
            group.MapGet("/{fileId}/photometry/sources", MeasureAllSources)
                .WithSummary("Measures aperture flux, instrumental magnitude, and uncertainty for every detected source in a staged image. Not yet implemented.");
        }
    }

    private static IResult MeasureAllSources(
        string fileId,
        double thresholdSigma = SourceDetector.DefaultThresholdSigma,
        int minimumArea = SourceDetector.DefaultMinimumArea,
        int maxSources = SourceDetector.DefaultMaxSources,
        double apertureRadius = MultiAperturePhotometryRequest.DefaultApertureRadius,
        double annulusInnerRadius = MultiAperturePhotometryRequest.DefaultAnnulusInnerRadius,
        double annulusOuterRadius = MultiAperturePhotometryRequest.DefaultAnnulusOuterRadius,
        double magnitudeZeroPoint = MultiAperturePhotometryRequest.DefaultMagnitudeZeroPoint)
    {
        _ = MultiAperturePhotometryRequest.Create(
            thresholdSigma, minimumArea, maxSources, apertureRadius, annulusInnerRadius, annulusOuterRadius, magnitudeZeroPoint);

        return NotImplementedResult.Value("images.multiphotometry.not_implemented", "Multi-source aperture photometry is not yet implemented.");
    }
}
