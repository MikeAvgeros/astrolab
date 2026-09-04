namespace AstroLab.Api.Features.Measurements.RadialVelocity;

public sealed record RadialVelocityResponse
{
    private RadialVelocityResponse(string fileId, double radialVelocityKmPerSec)
    {
        FileId = fileId;
        RadialVelocityKmPerSec = radialVelocityKmPerSec;
    }

    public string FileId { get; }

    public double RadialVelocityKmPerSec { get; }

    public static RadialVelocityResponse Create(string fileId, double radialVelocityKmPerSec)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new RadialVelocityResponse(fileId, radialVelocityKmPerSec);
    }
}
