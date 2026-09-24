using System.Collections.Immutable;

namespace AstroLab.Core.Imaging;

public readonly record struct ImageBackgroundModel
{
    private const double MeshCenterOffset = 0.5;

    private ImageBackgroundModel(
        int meshSizePixels, int meshCountX, int meshCountY, double medianBackground, double backgroundRms,
        ImmutableArray<double> meshBackgrounds, ImmutableArray<double> meshRms)
    {
        MeshSizePixels = meshSizePixels;
        MeshCountX = meshCountX;
        MeshCountY = meshCountY;
        MedianBackground = medianBackground;
        BackgroundRms = backgroundRms;
        MeshBackgrounds = meshBackgrounds;
        MeshRms = meshRms;
    }

    public int MeshSizePixels { get; }

    public int MeshCountX { get; }

    public int MeshCountY { get; }

    public double MedianBackground { get; }

    public double BackgroundRms { get; }

    public ImmutableArray<double> MeshBackgrounds { get; }

    public ImmutableArray<double> MeshRms { get; }

    public static ImageBackgroundModel Create(
        int meshSizePixels, int meshCountX, int meshCountY, double medianBackground, double backgroundRms,
        ImmutableArray<double> meshBackgrounds, ImmutableArray<double> meshRms)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(meshSizePixels);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(meshCountX);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(meshCountY);

        ArgumentOutOfRangeException.ThrowIfNegative(backgroundRms);

        ArgumentOutOfRangeException.ThrowIfNotEqual(meshBackgrounds.Length, meshCountX * meshCountY);

        ArgumentOutOfRangeException.ThrowIfNotEqual(meshRms.Length, meshCountX * meshCountY);

        return new ImageBackgroundModel(meshSizePixels, meshCountX, meshCountY, medianBackground, backgroundRms, meshBackgrounds, meshRms);
    }

    public double BackgroundAt(double x, double y) => Interpolate(MeshBackgrounds, x, y);

    public double RmsAt(double x, double y) => Interpolate(MeshRms, x, y);

    private double Interpolate(ImmutableArray<double> meshValues, double x, double y)
    {
        var gridX = Math.Clamp(x / MeshSizePixels - MeshCenterOffset, 0.0, MeshCountX - 1);

        var gridY = Math.Clamp(y / MeshSizePixels - MeshCenterOffset, 0.0, MeshCountY - 1);

        var x0 = (int)gridX;

        var y0 = (int)gridY;

        var x1 = Math.Min(x0 + 1, MeshCountX - 1);

        var y1 = Math.Min(y0 + 1, MeshCountY - 1);

        var fx = gridX - x0;

        var fy = gridY - y0;

        var bottom = meshValues[y0 * MeshCountX + x0] * (1.0 - fx) + meshValues[y0 * MeshCountX + x1] * fx;

        var top = meshValues[y1 * MeshCountX + x0] * (1.0 - fx) + meshValues[y1 * MeshCountX + x1] * fx;

        return bottom * (1.0 - fy) + top * fy;
    }
}
