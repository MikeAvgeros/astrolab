using AstroLab.Core.Sources;

namespace AstroLab.Api.Features.Images.SourceCharacterization;

public sealed record SourceCharacterizationRequest
{
    private SourceCharacterizationRequest(
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

    public static SourceCharacterizationRequest Create(
        double thresholdSigma = SourceDetector.DefaultThresholdSigma,
        int minimumArea = SourceDetector.DefaultMinimumArea,
        int maxSources = SourceDetector.DefaultMaxSources) =>
        new(thresholdSigma, minimumArea, maxSources);
}
