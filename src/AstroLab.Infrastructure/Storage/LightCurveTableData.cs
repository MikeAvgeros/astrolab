namespace AstroLab.Infrastructure.Storage;

public sealed record LightCurveTableData
{
    private LightCurveTableData(double[] time, double[] flux)
    {
        Time = time;
        Flux = flux;
    }

    public double[] Time { get; }

    public double[] Flux { get; }

    public static LightCurveTableData Create(double[] time, double[] flux)
    {
        ArgumentNullException.ThrowIfNull(time);

        ArgumentNullException.ThrowIfNull(flux);

        if (time.Length != flux.Length)
        {
            throw new ArgumentException(
                $"Time column ({time.Length} rows) and flux column ({flux.Length} rows) must have the same length.");
        }

        return new LightCurveTableData(time, flux);
    }
}
