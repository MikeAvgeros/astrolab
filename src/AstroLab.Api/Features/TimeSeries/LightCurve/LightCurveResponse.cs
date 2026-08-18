namespace AstroLab.Api.Features.TimeSeries.LightCurve;

public sealed record LightCurveResponse(string FileId, double[] Time, double[] Flux);
