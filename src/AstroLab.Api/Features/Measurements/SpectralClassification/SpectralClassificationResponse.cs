namespace AstroLab.Api.Features.Measurements.SpectralClassification;

public sealed record SpectralClassificationResponse
{
    private SpectralClassificationResponse(string fileId, string estimatedSpectralType, double confidence)
    {
        FileId = fileId;
        EstimatedSpectralType = estimatedSpectralType;
        Confidence = confidence;
    }

    public string FileId { get; }

    public string EstimatedSpectralType { get; }

    public double Confidence { get; }

    public static SpectralClassificationResponse Create(string fileId, string estimatedSpectralType, double confidence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentException.ThrowIfNullOrWhiteSpace(estimatedSpectralType);

        return new SpectralClassificationResponse(fileId, estimatedSpectralType, confidence);
    }
}
