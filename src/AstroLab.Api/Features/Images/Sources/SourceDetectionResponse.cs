namespace AstroLab.Api.Features.Images.Sources;

public sealed record SourceDetectionResponse(string FileId, IReadOnlyList<DetectedSourceDto> Sources);
