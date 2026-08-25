namespace AstroLab.Api.Features.Fits.Upload;

public sealed record FitsUploadResponse(string FileId, long SizeBytes);

/// <summary>Static factory accompanying <see cref="FitsUploadResponse"/>. Validates arguments before constructing.</summary>
public static class FitsUploadResponseFactory
{
    public static FitsUploadResponse Create(string fileId, long sizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);

        return new FitsUploadResponse(fileId, sizeBytes);
    }
}
