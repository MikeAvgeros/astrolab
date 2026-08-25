using AstroLab.Core.Fits;

namespace AstroLab.Infrastructure.Storage;

/// <summary>
/// An <see cref="HduDescriptor"/> paired with the byte offset, within its staged file, where that
/// HDU's data segment begins. File-layout detail owned by the imperative shell — <c>AstroLab.Core</c>
/// has no notion of "where in a stream" a HDU lives.
/// </summary>
/// <param name="Descriptor">The parsed HDU metadata.</param>
/// <param name="DataOffset">The byte offset, from the start of the stream, of this HDU's data segment.</param>
public readonly record struct HduLocation(HduDescriptor Descriptor, long DataOffset);

/// <summary>Static factory accompanying <see cref="HduLocation"/>. Validates arguments before constructing.</summary>
public static class HduLocationFactory
{
    public static HduLocation Create(HduDescriptor descriptor, long dataOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dataOffset);

        return new HduLocation(descriptor, dataOffset);
    }
}
