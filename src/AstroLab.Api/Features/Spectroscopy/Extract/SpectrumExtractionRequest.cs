using System.Collections.Immutable;
using AstroLab.Core.Spectroscopy;

namespace AstroLab.Api.Features.Spectroscopy.Extract;

public sealed record SpectrumExtractionRequest(DispersionAxis Axis, ImmutableList<double> TraceCenters, double ApertureHalfWidth, ImmutableList<double>? DispersionCoefficients = null);

/// <summary>Static factory accompanying <see cref="SpectrumExtractionRequest"/>. Validates arguments before constructing.</summary>
public static class SpectrumExtractionRequestFactory
{
    public static SpectrumExtractionRequest Create(DispersionAxis axis, ImmutableList<double> traceCenters, double apertureHalfWidth, ImmutableList<double>? dispersionCoefficients = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(apertureHalfWidth);

        return new SpectrumExtractionRequest(axis, traceCenters, apertureHalfWidth, dispersionCoefficients);
    }
}
