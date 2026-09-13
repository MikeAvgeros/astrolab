using System.Text.Json.Serialization;
using AstroLab.Core.Photometry;

namespace AstroLab.Api.Features.Images.Uncertainty;

public sealed record UncertaintyRequest
{
    [JsonConstructor]
    private UncertaintyRequest(
        double centerX, double centerY, double apertureRadius, double annulusInnerRadius, double annulusOuterRadius,
        BackgroundEstimationMethod backgroundMethod = BackgroundEstimationMethod.Median,
        double? detectorGain = null, double? readNoiseElectrons = null)
    {
        CenterX = centerX;
        CenterY = centerY;
        ApertureRadius = apertureRadius;
        AnnulusInnerRadius = annulusInnerRadius;
        AnnulusOuterRadius = annulusOuterRadius;
        BackgroundMethod = backgroundMethod;
        DetectorGain = detectorGain;
        ReadNoiseElectrons = readNoiseElectrons;
    }

    public double CenterX { get; }

    public double CenterY { get; }

    public double ApertureRadius { get; }

    public double AnnulusInnerRadius { get; }

    public double AnnulusOuterRadius { get; }

    public BackgroundEstimationMethod BackgroundMethod { get; }

    public double? DetectorGain { get; }

    public double? ReadNoiseElectrons { get; }

    public static UncertaintyRequest Create(
        double centerX, double centerY, double apertureRadius, double annulusInnerRadius, double annulusOuterRadius,
        BackgroundEstimationMethod backgroundMethod = BackgroundEstimationMethod.Median,
        double? detectorGain = null, double? readNoiseElectrons = null)
    {
        var request = new UncertaintyRequest(
            centerX, centerY, apertureRadius, annulusInnerRadius, annulusOuterRadius, backgroundMethod, detectorGain, readNoiseElectrons);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ApertureRadius);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(AnnulusInnerRadius);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(AnnulusOuterRadius);

        if (DetectorGain is { } gain)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gain);
        }

        if (ReadNoiseElectrons is { } readNoise)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(readNoise);
        }
    }
}
