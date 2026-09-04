namespace AstroLab.Api.Features.Images.DifferentialPhotometry;

public sealed record DifferentialPhotometryResponse
{
    private DifferentialPhotometryResponse(string fileId, double targetMagnitude, double comparisonMagnitude, double differentialMagnitude, double uncertainty)
    {
        FileId = fileId;
        TargetMagnitude = targetMagnitude;
        ComparisonMagnitude = comparisonMagnitude;
        DifferentialMagnitude = differentialMagnitude;
        Uncertainty = uncertainty;
    }

    public string FileId { get; }

    public double TargetMagnitude { get; }

    public double ComparisonMagnitude { get; }

    public double DifferentialMagnitude { get; }

    public double Uncertainty { get; }

    public static DifferentialPhotometryResponse Create(string fileId, double targetMagnitude, double comparisonMagnitude, double differentialMagnitude, double uncertainty)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new DifferentialPhotometryResponse(fileId, targetMagnitude, comparisonMagnitude, differentialMagnitude, uncertainty);
    }
}
