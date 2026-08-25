namespace AstroLab.Api.Features.Images.Sources;

public sealed record SourceDetectionRequest(double? DetectionThreshold = null, double? MinSeparation = null);

/// <summary>Static factory accompanying <see cref="SourceDetectionRequest"/>.</summary>
public static class SourceDetectionRequestFactory
{
    public static SourceDetectionRequest Create(double? detectionThreshold = null, double? minSeparation = null) =>
        new(detectionThreshold, minSeparation);
}
