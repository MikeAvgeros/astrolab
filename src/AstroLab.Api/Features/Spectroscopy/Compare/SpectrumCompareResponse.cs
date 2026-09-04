namespace AstroLab.Api.Features.Spectroscopy.Compare;

public sealed record SpectrumCompareResponse
{
    private SpectrumCompareResponse(string fileId, string comparisonFileId, double crossCorrelationPeak, double velocityShiftKmPerSec)
    {
        FileId = fileId;
        ComparisonFileId = comparisonFileId;
        CrossCorrelationPeak = crossCorrelationPeak;
        VelocityShiftKmPerSec = velocityShiftKmPerSec;
    }

    public string FileId { get; }

    public string ComparisonFileId { get; }

    public double CrossCorrelationPeak { get; }

    public double VelocityShiftKmPerSec { get; }

    public static SpectrumCompareResponse Create(string fileId, string comparisonFileId, double crossCorrelationPeak, double velocityShiftKmPerSec)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentException.ThrowIfNullOrWhiteSpace(comparisonFileId);

        return new SpectrumCompareResponse(fileId, comparisonFileId, crossCorrelationPeak, velocityShiftKmPerSec);
    }
}
