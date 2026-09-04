using AstroLab.Core.Sources;

namespace AstroLab.Api.Features.Images.Overlay;

public sealed record OverlayRenderRequest
{
    private OverlayRenderRequest(
        double thresholdSigma = SourceDetector.DefaultThresholdSigma,
        int minimumArea = SourceDetector.DefaultMinimumArea,
        int maxSources = SourceDetector.DefaultMaxSources)
    {
        ThresholdSigma = thresholdSigma;
        MinimumArea = minimumArea;
        MaxSources = maxSources;
    }

    public double ThresholdSigma { get; }

    public int MinimumArea { get; }

    public int MaxSources { get; }

    public static OverlayRenderRequest Create(
        double thresholdSigma = SourceDetector.DefaultThresholdSigma,
        int minimumArea = SourceDetector.DefaultMinimumArea,
        int maxSources = SourceDetector.DefaultMaxSources) =>
        new(thresholdSigma, minimumArea, maxSources);
}
