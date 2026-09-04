namespace AstroLab.Infrastructure.Archives;

/// <summary>Converts between Modified Julian Date (the epoch ESO's TAP responses report time in) and <see cref="DateTimeOffset"/>.</summary>
internal static class ModifiedJulianDate
{
    private static readonly DateTimeOffset Epoch = new(1858, 11, 17, 0, 0, 0, TimeSpan.Zero);

    public static DateTimeOffset ToDateTimeOffset(double? mjd)
    {
        if (mjd is null or <= 0)
        {
            return DateTimeOffset.UtcNow;
        }

        return Epoch.AddDays(mjd.Value);
    }

    public static double FromDateTimeOffset(DateTimeOffset value) => (value - Epoch).TotalDays;
}
