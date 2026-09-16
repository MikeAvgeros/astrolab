using System.Collections.Immutable;
using AstroLab.Core.Astrometry;

namespace AstroLab.Api.Features.Images.Astrometry;

public sealed record WcsValidationResponse
{
    private WcsValidationResponse(
        string fileId, bool isValid, bool isInvertible, double determinant, double skewDegrees,
        double pixelScaleRatio, double roundTripErrorPixels, ImmutableList<string> issues)
    {
        FileId = fileId;
        IsValid = isValid;
        IsInvertible = isInvertible;
        Determinant = determinant;
        SkewDegrees = skewDegrees;
        PixelScaleRatio = pixelScaleRatio;
        RoundTripErrorPixels = roundTripErrorPixels;
        Issues = issues;
    }

    public string FileId { get; }

    public bool IsValid { get; }

    public bool IsInvertible { get; }

    public double Determinant { get; }

    public double SkewDegrees { get; }

    public double PixelScaleRatio { get; }

    public double RoundTripErrorPixels { get; }

    public ImmutableList<string> Issues { get; }

    public static WcsValidationResponse Create(string fileId, WcsValidationReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new WcsValidationResponse(
            fileId, report.IsValid, report.IsInvertible, report.Determinant, report.SkewDegrees,
            report.PixelScaleRatio, report.RoundTripErrorPixels, [.. report.Issues]);
    }
}
