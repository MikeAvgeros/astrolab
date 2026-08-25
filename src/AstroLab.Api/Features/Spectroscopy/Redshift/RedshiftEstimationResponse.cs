namespace AstroLab.Api.Features.Spectroscopy.Redshift;

public sealed record RedshiftEstimationResponse(string FileId, double Redshift, double Uncertainty);

/// <summary>Static factory accompanying <see cref="RedshiftEstimationResponse"/>. Validates arguments before constructing.</summary>
public static class RedshiftEstimationResponseFactory
{
    public static RedshiftEstimationResponse Create(string fileId, double redshift, double uncertainty)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new RedshiftEstimationResponse(fileId, redshift, uncertainty);
    }
}
