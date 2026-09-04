using AstroLab.Core.Sources;

namespace AstroLab.Api.Features.Images.Segmentation;

public sealed record SegmentationRequest
{
    private SegmentationRequest(double thresholdSigma = SourceDetector.DefaultThresholdSigma, int minimumArea = SourceDetector.DefaultMinimumArea)
    {
        ThresholdSigma = thresholdSigma;
        MinimumArea = minimumArea;
    }

    public double ThresholdSigma { get; }

    public int MinimumArea { get; }

    public static SegmentationRequest Create(double thresholdSigma = SourceDetector.DefaultThresholdSigma, int minimumArea = SourceDetector.DefaultMinimumArea) =>
        new(thresholdSigma, minimumArea);
}
