using System.Collections.Immutable;
using System.Text.Json.Serialization;
using AstroLab.Core.Spectroscopy;

namespace AstroLab.Api.Features.Spectroscopy.Extract;

public sealed record SpectrumExtractionRequest
{
    [JsonConstructor]
    private SpectrumExtractionRequest(DispersionAxis axis, ImmutableList<double> traceCenters, double apertureHalfWidth, ImmutableList<double>? dispersionCoefficients = null)
    {
        Axis = axis;
        TraceCenters = traceCenters;
        ApertureHalfWidth = apertureHalfWidth;
        DispersionCoefficients = dispersionCoefficients;
    }

    public DispersionAxis Axis { get; }

    public ImmutableList<double> TraceCenters { get; }

    public double ApertureHalfWidth { get; }

    public ImmutableList<double>? DispersionCoefficients { get; }

    public static SpectrumExtractionRequest Create(DispersionAxis axis, ImmutableList<double> traceCenters, double apertureHalfWidth, ImmutableList<double>? dispersionCoefficients = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(apertureHalfWidth);

        return new SpectrumExtractionRequest(axis, traceCenters, apertureHalfWidth, dispersionCoefficients);
    }
}
