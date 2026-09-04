namespace AstroLab.Api.Features.Images.Background;

public sealed record BackgroundModelResponse
{
    private BackgroundModelResponse(string fileId, int meshSizePixels, double medianBackground, double backgroundRms)
    {
        FileId = fileId;
        MeshSizePixels = meshSizePixels;
        MedianBackground = medianBackground;
        BackgroundRms = backgroundRms;
    }

    public string FileId { get; }

    public int MeshSizePixels { get; }

    public double MedianBackground { get; }

    public double BackgroundRms { get; }

    public static BackgroundModelResponse Create(string fileId, int meshSizePixels, double medianBackground, double backgroundRms)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new BackgroundModelResponse(fileId, meshSizePixels, medianBackground, backgroundRms);
    }
}
