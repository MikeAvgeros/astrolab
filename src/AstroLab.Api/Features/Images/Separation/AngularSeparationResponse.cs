namespace AstroLab.Api.Features.Images.Separation;

public sealed record AngularSeparationResponse
{
    private AngularSeparationResponse(string fileId, double separationArcsec)
    {
        FileId = fileId;
        SeparationArcsec = separationArcsec;
    }

    public string FileId { get; }

    public double SeparationArcsec { get; }

    public static AngularSeparationResponse Create(string fileId, double separationArcsec)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new AngularSeparationResponse(fileId, separationArcsec);
    }
}
