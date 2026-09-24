using System.Text.Json.Serialization;
using AstroLab.Core.Photometry;

namespace AstroLab.Api.Features.Measurements.StellarColour;

public sealed record StellarColourRequest
{
    [JsonConstructor]
    private StellarColourRequest(
        string comparisonFileId, double centerX, double centerY, double apertureRadius, double annulusInnerRadius, double annulusOuterRadius,
        BackgroundEstimationMethod backgroundMethod = BackgroundEstimationMethod.Median)
    {
        ComparisonFileId = comparisonFileId;
        CenterX = centerX;
        CenterY = centerY;
        ApertureRadius = apertureRadius;
        AnnulusInnerRadius = annulusInnerRadius;
        AnnulusOuterRadius = annulusOuterRadius;
        BackgroundMethod = backgroundMethod;
    }

    public string ComparisonFileId { get; }

    public double CenterX { get; }

    public double CenterY { get; }

    public double ApertureRadius { get; }

    public double AnnulusInnerRadius { get; }

    public double AnnulusOuterRadius { get; }

    public BackgroundEstimationMethod BackgroundMethod { get; }

    public static StellarColourRequest Create(
        string comparisonFileId, double centerX, double centerY, double apertureRadius, double annulusInnerRadius, double annulusOuterRadius,
        BackgroundEstimationMethod backgroundMethod = BackgroundEstimationMethod.Median)
    {
        var request = new StellarColourRequest(
            comparisonFileId, centerX, centerY, apertureRadius, annulusInnerRadius, annulusOuterRadius, backgroundMethod);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ComparisonFileId);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ApertureRadius);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(AnnulusInnerRadius);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(AnnulusOuterRadius);
    }
}
