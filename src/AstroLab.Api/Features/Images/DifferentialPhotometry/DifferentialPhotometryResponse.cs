namespace AstroLab.Api.Features.Images.DifferentialPhotometry;

public sealed record DifferentialPhotometryResponse
{
    private DifferentialPhotometryResponse(
        string fileId, double targetMagnitude, double comparisonMagnitude, double differentialMagnitude, double uncertainty,
        double? targetSignalToNoiseRatio, double? comparisonSignalToNoiseRatio)
    {
        FileId = fileId;
        TargetMagnitude = targetMagnitude;
        ComparisonMagnitude = comparisonMagnitude;
        DifferentialMagnitude = differentialMagnitude;
        Uncertainty = uncertainty;
        TargetSignalToNoiseRatio = targetSignalToNoiseRatio;
        ComparisonSignalToNoiseRatio = comparisonSignalToNoiseRatio;
    }

    public string FileId { get; }

    public double TargetMagnitude { get; }

    public double ComparisonMagnitude { get; }

    public double DifferentialMagnitude { get; }

    public double Uncertainty { get; }

    public double? TargetSignalToNoiseRatio { get; }

    public double? ComparisonSignalToNoiseRatio { get; }

    public static DifferentialPhotometryResponse Create(
        string fileId, double targetMagnitude, double comparisonMagnitude, double differentialMagnitude, double uncertainty,
        double? targetSignalToNoiseRatio, double? comparisonSignalToNoiseRatio)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new DifferentialPhotometryResponse(
            fileId, targetMagnitude, comparisonMagnitude, differentialMagnitude, uncertainty,
            targetSignalToNoiseRatio, comparisonSignalToNoiseRatio);
    }
}
