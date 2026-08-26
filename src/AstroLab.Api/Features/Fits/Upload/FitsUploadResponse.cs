namespace AstroLab.Api.Features.Fits.Upload;

public sealed record FitsUploadResponse
{
    private FitsUploadResponse(string fileId, long sizeBytes)
    {
        FileId = fileId;
        SizeBytes = sizeBytes;
    }

    public string FileId { get; }

    public long SizeBytes { get; }

    public static FitsUploadResponse Create(string fileId, long sizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);

        return new FitsUploadResponse(fileId, sizeBytes);
    }
}
