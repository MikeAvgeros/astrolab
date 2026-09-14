namespace AstroLab.Api.Features.Measurements.RadialVelocity;

public sealed record RadialVelocityResponse
{
    private RadialVelocityResponse(string fileId, double radialVelocityKmPerSec, string method)
    {
        FileId = fileId;
        RadialVelocityKmPerSec = radialVelocityKmPerSec;
        Method = method;
    }

    public string FileId { get; }

    public double RadialVelocityKmPerSec { get; }

    public string Method { get; }

    public static RadialVelocityResponse Create(string fileId, double radialVelocityKmPerSec, string method)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        return new RadialVelocityResponse(fileId, radialVelocityKmPerSec, method);
    }
}
