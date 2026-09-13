namespace AstroLab.Api.Features.Spectroscopy.EquivalentWidth;

public sealed record EquivalentWidthResponse
{
    private EquivalentWidthResponse(
        string fileId, double equivalentWidth, double minWavelength, double maxWavelength, bool isWavelengthCalibrated)
    {
        FileId = fileId;
        EquivalentWidth = equivalentWidth;
        MinWavelength = minWavelength;
        MaxWavelength = maxWavelength;
        IsWavelengthCalibrated = isWavelengthCalibrated;
    }

    public string FileId { get; }

    public double EquivalentWidth { get; }

    public double MinWavelength { get; }

    public double MaxWavelength { get; }

    public bool IsWavelengthCalibrated { get; }

    public static EquivalentWidthResponse Create(
        string fileId, double equivalentWidth, double minWavelength, double maxWavelength, bool isWavelengthCalibrated)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new EquivalentWidthResponse(fileId, equivalentWidth, minWavelength, maxWavelength, isWavelengthCalibrated);
    }
}
