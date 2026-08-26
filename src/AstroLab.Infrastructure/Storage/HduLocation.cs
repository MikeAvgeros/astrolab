using AstroLab.Core.Fits;

namespace AstroLab.Infrastructure.Storage;

/// <summary>
/// An <see cref="HduDescriptor"/> paired with the byte offset, within its staged file, where that
/// HDU's data segment begins. File-layout detail owned by the imperative shell — <c>AstroLab.Core</c>
/// has no notion of "where in a stream" a HDU lives.
/// </summary>
public readonly record struct HduLocation
{
    private HduLocation(HduDescriptor descriptor, long dataOffset)
    {
        Descriptor = descriptor;
        DataOffset = dataOffset;
    }

    /// <summary>The parsed HDU metadata.</summary>
    public HduDescriptor Descriptor { get; }

    /// <summary>The byte offset, from the start of the stream, of this HDU's data segment.</summary>
    public long DataOffset { get; }

    public static HduLocation Create(HduDescriptor descriptor, long dataOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dataOffset);

        return new HduLocation(descriptor, dataOffset);
    }
}
