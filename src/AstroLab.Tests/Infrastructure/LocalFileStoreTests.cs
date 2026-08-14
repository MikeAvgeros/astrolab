using System.IO.Pipelines;
using System.Text;
using AstroLab.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace AstroLab.Tests.Infrastructure;

public class LocalFileStoreTests : IDisposable
{
    private readonly string _root;
    private readonly LocalFileStore _store;

    public LocalFileStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "astrolab-tests-" + Guid.NewGuid().ToString("N"));
        _store = new LocalFileStore(Options.Create(new LocalFileStoreOptions { RootPath = _root }));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static async Task<PipeReader> CreateReaderAsync(byte[] content)
    {
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(content);
        await pipe.Writer.CompleteAsync();
        return pipe.Reader;
    }

    [Fact]
    public async Task WriteAsync_ThenOpenRead_RoundTripsExactBytes()
    {
        var payload = Encoding.UTF8.GetBytes("SIMPLE  =                    T / FITS-like payload");
        var reader = await CreateReaderAsync(payload);

        var writeResult = await _store.WriteAsync("dataset.fits", reader);

        Assert.True(writeResult.IsSuccess);
        Assert.Equal(payload.Length, writeResult.Value.SizeBytes);

        var openResult = _store.OpenRead("dataset.fits");
        Assert.True(openResult.IsSuccess);
        await using var stream = openResult.Value;
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);

        Assert.Equal(payload, memory.ToArray());
    }

    [Theory]
    [InlineData("../outside.fits")]
    [InlineData("../../etc/passwd")]
    [InlineData("nested/../../escape.fits")]
    public void ResolvePath_RejectsPathTraversal(string maliciousKey)
    {
        var result = _store.ResolvePath(maliciousKey);

        Assert.True(result.IsFailure);
        Assert.Equal("storage.path_traversal_rejected", result.Error.Code);
    }

    [Fact]
    public void ResolvePath_AllowsNestedSubdirectories()
    {
        var result = _store.ResolvePath("eso/2026/dataset.fits");

        Assert.True(result.IsSuccess);
        Assert.StartsWith(_root, result.Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Exists_ReflectsWriteAndDelete()
    {
        Assert.False(_store.Exists("temp.fits"));

        var reader = await CreateReaderAsync([1, 2, 3]);
        await _store.WriteAsync("temp.fits", reader);
        Assert.True(_store.Exists("temp.fits"));

        var deleteResult = _store.Delete("temp.fits");
        Assert.True(deleteResult.IsSuccess);
        Assert.False(_store.Exists("temp.fits"));
    }

    [Fact]
    public void OpenRead_MissingFile_ReturnsNotFound()
    {
        var result = _store.OpenRead("does-not-exist.fits");

        Assert.True(result.IsFailure);
        Assert.Equal("storage.file_not_found", result.Error.Code);
    }

    [Fact]
    public void CreateStagingKey_ProducesUniqueKeysWithExtension()
    {
        var first = _store.CreateStagingKey("fits");
        var second = _store.CreateStagingKey("fits");

        Assert.NotEqual(first, second);
        Assert.EndsWith(".fits", first);
    }
}
