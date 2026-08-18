namespace AstroLab.Api.Features.Images.Sources;

public sealed record SourceDetectionRequest(double? DetectionThreshold = null, double? MinSeparation = null);
