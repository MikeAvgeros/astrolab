using AstroLab.Core.Photometry;

namespace AstroLab.Api.Features.Images.Photometry;

public sealed record AperturePhotometryRequest(double CenterX, double CenterY, double ApertureRadius, double AnnulusInnerRadius, double AnnulusOuterRadius, BackgroundEstimationMethod BackgroundMethod = BackgroundEstimationMethod.Median);

/// <summary>Static factory accompanying <see cref="AperturePhotometryRequest"/>. Validates arguments before constructing.</summary>
public static class AperturePhotometryRequestFactory
{
    public static AperturePhotometryRequest Create(double centerX, double centerY, double apertureRadius, double annulusInnerRadius, double annulusOuterRadius, BackgroundEstimationMethod backgroundMethod = BackgroundEstimationMethod.Median)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(apertureRadius);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(annulusInnerRadius);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(annulusOuterRadius);

        return new AperturePhotometryRequest(centerX, centerY, apertureRadius, annulusInnerRadius, annulusOuterRadius, backgroundMethod);
    }
}
