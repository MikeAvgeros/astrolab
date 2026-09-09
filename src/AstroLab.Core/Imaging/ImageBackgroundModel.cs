namespace AstroLab.Core.Imaging;

public readonly record struct ImageBackgroundModel
{
    private ImageBackgroundModel(int meshSizePixels, int meshCountX, int meshCountY, double medianBackground, double backgroundRms)
    {
        MeshSizePixels = meshSizePixels;
        MeshCountX = meshCountX;
        MeshCountY = meshCountY;
        MedianBackground = medianBackground;
        BackgroundRms = backgroundRms;
    }

    public int MeshSizePixels { get; }

    public int MeshCountX { get; }

    public int MeshCountY { get; }

    public double MedianBackground { get; }

    public double BackgroundRms { get; }

    public static ImageBackgroundModel Create(int meshSizePixels, int meshCountX, int meshCountY, double medianBackground, double backgroundRms)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(meshSizePixels);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(meshCountX);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(meshCountY);

        ArgumentOutOfRangeException.ThrowIfNegative(backgroundRms);

        return new ImageBackgroundModel(meshSizePixels, meshCountX, meshCountY, medianBackground, backgroundRms);
    }
}
