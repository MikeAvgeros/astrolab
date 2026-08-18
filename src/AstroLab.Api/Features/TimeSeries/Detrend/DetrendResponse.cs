namespace AstroLab.Api.Features.TimeSeries.Detrend;

public sealed record DetrendResponse(string FileId, double[] Time, double[] DetrendedFlux);
