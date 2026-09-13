using System.Collections.Immutable;

namespace AstroLab.Core.Astrometry;

public readonly record struct WcsValidationReport
{
    private WcsValidationReport(
        bool isInvertible, double determinant, double skewDegrees, double pixelScaleRatio,
        double roundTripErrorPixels, ImmutableArray<string> issues)
    {
        IsInvertible = isInvertible;
        Determinant = determinant;
        SkewDegrees = skewDegrees;
        PixelScaleRatio = pixelScaleRatio;
        RoundTripErrorPixels = roundTripErrorPixels;
        Issues = issues;
    }

    public bool IsInvertible { get; }

    public double Determinant { get; }

    public double SkewDegrees { get; }

    public double PixelScaleRatio { get; }

    public double RoundTripErrorPixels { get; }

    public ImmutableArray<string> Issues { get; }

    public bool IsValid => Issues.IsEmpty;

    public static WcsValidationReport Create(
        bool isInvertible, double determinant, double skewDegrees, double pixelScaleRatio,
        double roundTripErrorPixels, ImmutableArray<string> issues) =>
        new(isInvertible, determinant, skewDegrees, pixelScaleRatio, roundTripErrorPixels, issues);
}
