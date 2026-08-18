namespace AstroLab.Api.Features.TimeSeries.PeriodSearch;

public sealed record PeriodSearchResponse(string FileId, double BestPeriod, double Power);
