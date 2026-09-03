using AstroLab.Core.Fits;

namespace AstroLab.Infrastructure.Storage;

public readonly record struct HduLocation
{
    private HduLocation(HduDescriptor descriptor, long dataOffset)
    {
        Descriptor = descriptor;
        DataOffset = dataOffset;
    }

    public HduDescriptor Descriptor { get; }

    public long DataOffset { get; }

    public static HduLocation Create(HduDescriptor descriptor, long dataOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dataOffset);

        return new HduLocation(descriptor, dataOffset);
    }
}
