namespace AstroLab.Api.Features.Measurements.PhysicalSize;

public sealed record PhysicalSizeResponse
{
    private PhysicalSizeResponse(double angularSizeArcsec, double distanceParsecs, double physicalSizeAu)
    {
        AngularSizeArcsec = angularSizeArcsec;
        DistanceParsecs = distanceParsecs;
        PhysicalSizeAu = physicalSizeAu;
    }

    public double AngularSizeArcsec { get; }

    public double DistanceParsecs { get; }

    public double PhysicalSizeAu { get; }

    public static PhysicalSizeResponse Create(double angularSizeArcsec, double distanceParsecs, double physicalSizeAu)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(physicalSizeAu);

        return new PhysicalSizeResponse(angularSizeArcsec, distanceParsecs, physicalSizeAu);
    }
}
