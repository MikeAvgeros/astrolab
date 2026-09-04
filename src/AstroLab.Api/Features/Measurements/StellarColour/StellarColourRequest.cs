using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Measurements.StellarColour;

public sealed record StellarColourRequest
{
    [JsonConstructor]
    private StellarColourRequest(string comparisonFileId, double centerX, double centerY, double apertureRadius)
    {
        ComparisonFileId = comparisonFileId;
        CenterX = centerX;
        CenterY = centerY;
        ApertureRadius = apertureRadius;
    }

    public string ComparisonFileId { get; }

    public double CenterX { get; }

    public double CenterY { get; }

    public double ApertureRadius { get; }

    public static StellarColourRequest Create(string comparisonFileId, double centerX, double centerY, double apertureRadius)
    {
        var request = new StellarColourRequest(comparisonFileId, centerX, centerY, apertureRadius);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ComparisonFileId);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ApertureRadius);
    }
}
