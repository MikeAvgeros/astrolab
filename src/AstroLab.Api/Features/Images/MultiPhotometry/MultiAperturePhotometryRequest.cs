namespace AstroLab.Api.Features.Images.MultiPhotometry;

public sealed record MultiAperturePhotometryRequest
{
    internal const double DefaultApertureRadius = 5.0;
    internal const double DefaultAnnulusInnerRadius = 8.0;
    internal const double DefaultAnnulusOuterRadius = 12.0;
    internal const double DefaultMagnitudeZeroPoint = 25.0;

    private MultiAperturePhotometryRequest(
        double thresholdSigma,
        int minimumArea,
        int maxSources,
        double apertureRadius,
        double annulusInnerRadius,
        double annulusOuterRadius,
        double magnitudeZeroPoint)
    {
        ThresholdSigma = thresholdSigma;
        MinimumArea = minimumArea;
        MaxSources = maxSources;
        ApertureRadius = apertureRadius;
        AnnulusInnerRadius = annulusInnerRadius;
        AnnulusOuterRadius = annulusOuterRadius;
        MagnitudeZeroPoint = magnitudeZeroPoint;
    }

    public double ThresholdSigma { get; }

    public int MinimumArea { get; }

    public int MaxSources { get; }

    public double ApertureRadius { get; }

    public double AnnulusInnerRadius { get; }

    public double AnnulusOuterRadius { get; }

    public double MagnitudeZeroPoint { get; }

    public static MultiAperturePhotometryRequest Create(
        double thresholdSigma,
        int minimumArea,
        int maxSources,
        double apertureRadius,
        double annulusInnerRadius,
        double annulusOuterRadius,
        double magnitudeZeroPoint)
    {
        var request = new MultiAperturePhotometryRequest(
            thresholdSigma, minimumArea, maxSources, apertureRadius, annulusInnerRadius, annulusOuterRadius, magnitudeZeroPoint);

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
