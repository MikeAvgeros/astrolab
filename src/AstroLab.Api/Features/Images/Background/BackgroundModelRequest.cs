namespace AstroLab.Api.Features.Images.Background;

public sealed record BackgroundModelRequest
{
    internal const int DefaultMeshSizePixels = 64;

    private BackgroundModelRequest(int meshSizePixels)
    {
        MeshSizePixels = meshSizePixels;
    }

    public int MeshSizePixels { get; }

    public static BackgroundModelRequest Create(int meshSizePixels)
    {
        var request = new BackgroundModelRequest(meshSizePixels);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MeshSizePixels);
    }
}
