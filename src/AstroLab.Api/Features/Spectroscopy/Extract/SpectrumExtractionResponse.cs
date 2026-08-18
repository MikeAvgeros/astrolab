namespace AstroLab.Api.Features.Spectroscopy.Extract;

public sealed record SpectrumExtractionResponse(string FileId, double[]? Wavelengths, double[] Flux);
