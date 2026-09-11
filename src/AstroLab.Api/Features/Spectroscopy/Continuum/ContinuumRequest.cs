using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Spectroscopy.Continuum;

public sealed record ContinuumRequest
{
    [JsonConstructor]
    private ContinuumRequest(
        int polynomialDegree,
        WavelengthRangeDto[]? excludedRanges,
        double? sigmaClipThreshold,
        int? sigmaClipIterations)
    {
        PolynomialDegree = polynomialDegree;
        ExcludedRanges = excludedRanges;
        SigmaClipThreshold = sigmaClipThreshold;
        SigmaClipIterations = sigmaClipIterations;
    }

    public int PolynomialDegree { get; }

    public WavelengthRangeDto[]? ExcludedRanges { get; }

    public double? SigmaClipThreshold { get; }

    public int? SigmaClipIterations { get; }

    public static ContinuumRequest Create(
        int polynomialDegree,
        WavelengthRangeDto[]? excludedRanges = null,
        double? sigmaClipThreshold = null,
        int? sigmaClipIterations = null)
    {
        var request = new ContinuumRequest(polynomialDegree, excludedRanges, sigmaClipThreshold, sigmaClipIterations);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(PolynomialDegree);

        if (SigmaClipThreshold is { } threshold)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(threshold);
        }

        if (SigmaClipIterations is { } iterations)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);
        }
    }
}
