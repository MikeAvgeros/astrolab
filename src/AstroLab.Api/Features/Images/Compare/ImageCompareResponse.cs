namespace AstroLab.Api.Features.Images.Compare;

public sealed record ImageCompareResponse
{
    private ImageCompareResponse(string fileId, string comparisonFileId, double meanDifference, double standardDeviationDifference, double maxAbsoluteDifference)
    {
        FileId = fileId;
        ComparisonFileId = comparisonFileId;
        MeanDifference = meanDifference;
        StandardDeviationDifference = standardDeviationDifference;
        MaxAbsoluteDifference = maxAbsoluteDifference;
    }

    public string FileId { get; }

    public string ComparisonFileId { get; }

    public double MeanDifference { get; }

    public double StandardDeviationDifference { get; }

    public double MaxAbsoluteDifference { get; }

    public static ImageCompareResponse Create(string fileId, string comparisonFileId, double meanDifference, double standardDeviationDifference, double maxAbsoluteDifference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentException.ThrowIfNullOrWhiteSpace(comparisonFileId);

        return new ImageCompareResponse(fileId, comparisonFileId, meanDifference, standardDeviationDifference, maxAbsoluteDifference);
    }
}
