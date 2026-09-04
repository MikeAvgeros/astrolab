using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Images.DifferentialPhotometry;

public sealed record DifferentialPhotometryRequest
{
    [JsonConstructor]
    private DifferentialPhotometryRequest(
        double targetCenterX,
        double targetCenterY,
        double comparisonCenterX,
        double comparisonCenterY,
        double apertureRadius,
        double annulusInnerRadius,
        double annulusOuterRadius)
    {
        TargetCenterX = targetCenterX;
        TargetCenterY = targetCenterY;
        ComparisonCenterX = comparisonCenterX;
        ComparisonCenterY = comparisonCenterY;
        ApertureRadius = apertureRadius;
        AnnulusInnerRadius = annulusInnerRadius;
        AnnulusOuterRadius = annulusOuterRadius;
    }

    public double TargetCenterX { get; }

    public double TargetCenterY { get; }

    public double ComparisonCenterX { get; }

    public double ComparisonCenterY { get; }

    public double ApertureRadius { get; }

    public double AnnulusInnerRadius { get; }

    public double AnnulusOuterRadius { get; }

    public static DifferentialPhotometryRequest Create(
        double targetCenterX,
        double targetCenterY,
        double comparisonCenterX,
        double comparisonCenterY,
        double apertureRadius,
        double annulusInnerRadius,
        double annulusOuterRadius)
    {
        var request = new DifferentialPhotometryRequest(
            targetCenterX, targetCenterY, comparisonCenterX, comparisonCenterY, apertureRadius, annulusInnerRadius, annulusOuterRadius);

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
