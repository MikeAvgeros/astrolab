using System.IO.Pipelines;

namespace AstroLab.Infrastructure.Archives;

/// <summary>
/// A streamed dataset download in progress. Owns the underlying <see cref="HttpResponseMessage"/>
/// for the lifetime of the read — disposing an <see cref="ArchiveDownload"/> completes
/// <see cref="Content"/> and releases the HTTP response, so callers should consume <see cref="Content"/>
/// (typically via <c>ILocalFileStore.WriteAsync</c>) inside an <c>await using</c> block.
/// </summary>
public sealed class ArchiveDownload : IAsyncDisposable
{
    private readonly HttpResponseMessage _response;

    public ArchiveDownload(string suggestedFileName, long? contentLength, PipeReader content, HttpResponseMessage response)
    {
        SuggestedFileName = suggestedFileName;
        ContentLength = contentLength;
        Content = content;
        _response = response;
    }

    /// <summary>A reasonable file name (with extension) suggested by the archive for the downloaded dataset.</summary>
    public string SuggestedFileName { get; }

    /// <summary>The declared content length, if the archive provided one.</summary>
    public long? ContentLength { get; }

    /// <summary>The streamed dataset content, ready to be consumed exactly once.</summary>
    public PipeReader Content { get; }

    public async ValueTask DisposeAsync()
    {
        await Content.CompleteAsync();

        _response.Dispose();
    }
}
