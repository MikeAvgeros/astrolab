namespace AstroLab.Api.Features.TimeSeries.Transit;

public sealed record TransitRequest(double MinPeriod, double MaxPeriod, double MinTransitDepth);
