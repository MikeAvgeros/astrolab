namespace AstroLab.Api.Features.Measurements.PhysicalSize;

public sealed record PhysicalSizeResponse
{
    private PhysicalSizeResponse(double angularSizeArcsec, double distanceParsecs, double physicalSizeAu, string method)
    {
        AngularSizeArcsec = angularSizeArcsec;
        DistanceParsecs = distanceParsecs;
        PhysicalSizeAu = physicalSizeAu;
        Method = method;
    }

    public double AngularSizeArcsec { get; }

    public double DistanceParsecs { get; }

    public double PhysicalSizeAu { get; }

    public string Method { get; }

    public static PhysicalSizeResponse Create(double angularSizeArcsec, double distanceParsecs, double physicalSizeAu, string method)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(physicalSizeAu);

        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        return new PhysicalSizeResponse(angularSizeArcsec, distanceParsecs, physicalSizeAu, method);
    }
}
