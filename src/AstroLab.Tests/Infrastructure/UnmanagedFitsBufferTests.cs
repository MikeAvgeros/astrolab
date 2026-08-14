using AstroLab.Infrastructure.CFITSIO;

namespace AstroLab.Tests.Infrastructure;

public class UnmanagedFitsBufferTests
{
    [Fact]
    public void Allocate_ZeroInitializesMemory()
    {
        using var buffer = UnmanagedFitsBuffer.Allocate(16);

        Assert.All(buffer.AsSpan().ToArray(), b => Assert.Equal(0, b));
    }

    [Fact]
    public void CopyFrom_WritesBytesAtGivenOffset()
    {
        using var buffer = UnmanagedFitsBuffer.Allocate(8);
        ReadOnlySpan<byte> data = [1, 2, 3, 4];

        buffer.CopyFrom(data, destinationOffset: 2);

        var result = buffer.AsSpan().ToArray();
        Assert.Equal([0, 0, 1, 2, 3, 4, 0, 0], result);
    }

    [Fact]
    public void CopyFrom_OutOfBounds_Throws()
    {
        using var buffer = UnmanagedFitsBuffer.Allocate(4);

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.CopyFrom(new byte[8]));
    }

    [Fact]
    public void AsFloatSpan_ReinterpretsBytesAsFloats()
    {
        using var buffer = UnmanagedFitsBuffer.Allocate(sizeof(float) * 2);
        Span<byte> floatBytes = stackalloc byte[sizeof(float) * 2];
        BitConverter.TryWriteBytes(floatBytes, 1.5f);
        BitConverter.TryWriteBytes(floatBytes[sizeof(float)..], -2.5f);
        buffer.CopyFrom(floatBytes);

        var floats = buffer.AsFloatSpan();

        Assert.Equal(2, floats.Length);
        Assert.Equal(1.5f, floats[0]);
        Assert.Equal(-2.5f, floats[1]);
    }

    [Fact]
    public void Dispose_IsIdempotent_AndDoesNotDoubleFree()
    {
        var buffer = UnmanagedFitsBuffer.Allocate(16);

        buffer.Dispose();
        var exception = Record.Exception(() => buffer.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void AccessAfterDispose_ThrowsObjectDisposedException()
    {
        var buffer = UnmanagedFitsBuffer.Allocate(16);
        buffer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => buffer.AsSpan());
    }

    [Fact]
    public void Allocate_ZeroLength_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UnmanagedFitsBuffer.Allocate(0));
    }
}
