namespace AstroLab.Api.Features.Spectroscopy.Redshift;

public sealed record RedshiftEstimationResponse
{
    private RedshiftEstimationResponse(string fileId, double redshift, double uncertainty, string method)
    {
        FileId = fileId;
        Redshift = redshift;
        Uncertainty = uncertainty;
        Method = method;
    }

    public string FileId { get; }

    public double Redshift { get; }

    public double Uncertainty { get; }

    /// <summary>Either "line_pairs" or "cross_correlation", depending on which inputs were supplied.</summary>
    public string Method { get; }

    public static RedshiftEstimationResponse Create(string fileId, double redshift, double uncertainty, string method)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        return new RedshiftEstimationResponse(fileId, redshift, uncertainty, method);
    }
}
