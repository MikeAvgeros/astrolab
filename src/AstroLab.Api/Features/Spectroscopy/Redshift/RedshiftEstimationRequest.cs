namespace AstroLab.Api.Features.Spectroscopy.Redshift;

public sealed record RedshiftEstimationRequest(double[] ObservedWavelengths, double[] RestWavelengths);
