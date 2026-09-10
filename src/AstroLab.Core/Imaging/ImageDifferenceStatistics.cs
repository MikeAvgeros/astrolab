namespace AstroLab.Core.Imaging;

public readonly record struct ImageDifferenceStatistics
{
    private ImageDifferenceStatistics(double meanDifference, double standardDeviationDifference, double maxAbsoluteDifference)
    {
        MeanDifference = meanDifference;
        StandardDeviationDifference = standardDeviationDifference;
        MaxAbsoluteDifference = maxAbsoluteDifference;
    }

    public double MeanDifference { get; }

    public double StandardDeviationDifference { get; }

    public double MaxAbsoluteDifference { get; }

    public static ImageDifferenceStatistics Create(double meanDifference, double standardDeviationDifference, double maxAbsoluteDifference)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(standardDeviationDifference);

        ArgumentOutOfRangeException.ThrowIfNegative(maxAbsoluteDifference);

        return new ImageDifferenceStatistics(meanDifference, standardDeviationDifference, maxAbsoluteDifference);
    }
}
