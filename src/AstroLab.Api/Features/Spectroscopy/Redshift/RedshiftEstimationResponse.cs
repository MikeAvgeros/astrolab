namespace AstroLab.Api.Features.Spectroscopy.Redshift;

public sealed record RedshiftEstimationResponse
{
    private RedshiftEstimationResponse(string fileId, double redshift, double uncertainty)
    {
        FileId = fileId;
        Redshift = redshift;
        Uncertainty = uncertainty;
    }

    public string FileId { get; }

    public double Redshift { get; }

    public double Uncertainty { get; }

    public static RedshiftEstimationResponse Create(string fileId, double redshift, double uncertainty)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new RedshiftEstimationResponse(fileId, redshift, uncertainty);
    }
}
