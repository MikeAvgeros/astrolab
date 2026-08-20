namespace AstroLab.Api.Features.TimeSeries.Transit;

public sealed record TransitResponse(string FileId, double BestPeriod, double TransitDepth, double TransitDuration);
