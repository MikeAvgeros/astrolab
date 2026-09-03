using System.Text.Json.Serialization;
using AstroLab.Core.Photometry;

namespace AstroLab.Api.Features.Images.Photometry;

public sealed record AperturePhotometryRequest
{
    [JsonConstructor]
    private AperturePhotometryRequest(double centerX, double centerY, double apertureRadius, double annulusInnerRadius, double annulusOuterRadius, BackgroundEstimationMethod backgroundMethod = BackgroundEstimationMethod.Median)
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

    public static AperturePhotometryRequest Create(double centerX, double centerY, double apertureRadius, double annulusInnerRadius, double annulusOuterRadius, BackgroundEstimationMethod backgroundMethod = BackgroundEstimationMethod.Median)
    {
        var request = new AperturePhotometryRequest(centerX, centerY, apertureRadius, annulusInnerRadius, annulusOuterRadius, backgroundMethod);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ApertureRadius);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(AnnulusInnerRadius);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(AnnulusOuterRadius);
    }
}
