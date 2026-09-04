namespace AstroLab.Api.Features.Measurements.PhysicalSize;

public sealed record PhysicalSizeRequest
{
    private PhysicalSizeRequest(double angularSizeArcsec, double distanceParsecs)
    {
        AngularSizeArcsec = angularSizeArcsec;
        DistanceParsecs = distanceParsecs;
    }

    public double AngularSizeArcsec { get; }

    public double DistanceParsecs { get; }

    public static PhysicalSizeRequest Create(double angularSizeArcsec, double distanceParsecs)
    {
        var request = new PhysicalSizeRequest(angularSizeArcsec, distanceParsecs);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(AngularSizeArcsec);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(DistanceParsecs);
    }
}
