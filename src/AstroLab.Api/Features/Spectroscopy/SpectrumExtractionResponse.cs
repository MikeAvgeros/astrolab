namespace AstroLab.Api.Features.Spectroscopy;

public sealed record SpectrumExtractionResponse(string FileId, double[]? Wavelengths, double[] Flux);
