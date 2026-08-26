namespace AstroLab.Api.Features.Images.Sources;

public sealed record SourceDetectionRequest
{
    public SourceDetectionRequest(double? detectionThreshold = null, double? minSeparation = null)
    {
        DetectionThreshold = detectionThreshold;
        MinSeparation = minSeparation;
    }

    public double? DetectionThreshold { get; }

    public double? MinSeparation { get; }

    public static SourceDetectionRequest Create(double? detectionThreshold = null, double? minSeparation = null) =>
        new(detectionThreshold, minSeparation);
}
