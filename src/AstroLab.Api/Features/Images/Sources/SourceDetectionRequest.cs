using AstroLab.Core.Sources;

namespace AstroLab.Api.Features.Images.Sources;

public sealed record SourceDetectionRequest
{
    public SourceDetectionRequest(
        double thresholdSigma = SourceDetector.DefaultThresholdSigma,
        int minimumArea = SourceDetector.DefaultMinimumArea,
        int maxSources = SourceDetector.DefaultMaxSources)
    {
        ThresholdSigma = thresholdSigma;
        MinimumArea = minimumArea;
        MaxSources = maxSources;
    }

    /// <summary>Detection threshold, in multiples of the estimated background noise (sigma) above the estimated background.</summary>
    public double ThresholdSigma { get; }

    /// <summary>The minimum number of connected pixels a region must have to be reported as a source.</summary>
    public int MinimumArea { get; }

    /// <summary>The maximum number of sources returned, ranked by integrated flux (descending).</summary>
    public int MaxSources { get; }

    public static SourceDetectionRequest Create(
        double thresholdSigma = SourceDetector.DefaultThresholdSigma,
        int minimumArea = SourceDetector.DefaultMinimumArea,
        int maxSources = SourceDetector.DefaultMaxSources) =>
        new(thresholdSigma, minimumArea, maxSources);
}
