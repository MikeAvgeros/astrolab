using AstroLab.Core.Spectroscopy;

namespace AstroLab.Api.Features.Spectroscopy;

public sealed record SpectrumExtractionRequest(
    DispersionAxis Axis, double[] TraceCenters, double ApertureHalfWidth, double[]? DispersionCoefficients = null);
