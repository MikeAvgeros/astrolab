namespace AstroLab.Api.Features.Images.Statistics;

public sealed record PercentileDto
{
    private PercentileDto(double percentile, double value)
    {
        Percentile = percentile;
        Value = value;
    }

    public double Percentile { get; }

    public double Value { get; }

    public static PercentileDto Create(double percentile, double value) => new(percentile, value);
}
