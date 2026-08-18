namespace AstroLab.Api.Features.Spectroscopy.Redshift;

public sealed record RedshiftEstimationResponse(string FileId, double Redshift, double Uncertainty);
