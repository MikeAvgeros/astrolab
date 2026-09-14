namespace AstroLab.Api.Features.Measurements.SpectralClassification;

public sealed record SpectralClassificationResponse
{
    private SpectralClassificationResponse(string fileId, string estimatedSpectralType, double confidence, string method)
    {
        FileId = fileId;
        EstimatedSpectralType = estimatedSpectralType;
        Confidence = confidence;
        Method = method;
    }

    public string FileId { get; }

    public string EstimatedSpectralType { get; }

    public double Confidence { get; }

    public string Method { get; }

    public static SpectralClassificationResponse Create(string fileId, string estimatedSpectralType, double confidence, string method)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentException.ThrowIfNullOrWhiteSpace(estimatedSpectralType);

        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        return new SpectralClassificationResponse(fileId, estimatedSpectralType, confidence, method);
    }
}
