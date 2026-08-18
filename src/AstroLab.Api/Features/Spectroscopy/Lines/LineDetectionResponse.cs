namespace AstroLab.Api.Features.Spectroscopy.Lines;

public sealed record LineDetectionResponse(string FileId, IReadOnlyList<SpectralLineDto> Lines);
