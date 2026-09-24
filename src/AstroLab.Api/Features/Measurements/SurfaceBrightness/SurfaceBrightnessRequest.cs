using AstroLab.Core.Photometry;

namespace AstroLab.Api.Features.Measurements.SurfaceBrightness;

public sealed record SurfaceBrightnessRequest
{
    private SurfaceBrightnessRequest(
        double centerX, double centerY, double apertureRadius, double annulusInnerRadius, double annulusOuterRadius,
        BackgroundEstimationMethod backgroundMethod)
    {
        CenterX = centerX;
        CenterY = centerY;
        ApertureRadius = apertureRadius;
        AnnulusInnerRadius = annulusInnerRadius;
        AnnulusOuterRadius = annulusOuterRadius;
        BackgroundMethod = backgroundMethod;
    }

    public double CenterX { get; }

    public double CenterY { get; }

    public double ApertureRadius { get; }

    public double AnnulusInnerRadius { get; }

    public double AnnulusOuterRadius { get; }

    public BackgroundEstimationMethod BackgroundMethod { get; }

    public static SurfaceBrightnessRequest Create(
        double centerX, double centerY, double apertureRadius, double annulusInnerRadius, double annulusOuterRadius,
        BackgroundEstimationMethod backgroundMethod)
    {
        var request = new SurfaceBrightnessRequest(centerX, centerY, apertureRadius, annulusInnerRadius, annulusOuterRadius, backgroundMethod);

        request.Validate();

        return request;
    }

    private void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ApertureRadius);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(AnnulusInnerRadius);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(AnnulusOuterRadius);
    }
}
