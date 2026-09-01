using System.IO.Pipelines;

namespace AstroLab.Infrastructure.Archives;

public sealed class ArchiveDownload : IAsyncDisposable
{
    private readonly HttpResponseMessage _response;

    public ArchiveDownload(string fileName, long? contentLength, PipeReader content, HttpResponseMessage response)
    {
        FileName = fileName;
        ContentLength = contentLength;
        Content = content;
        _response = response;
    }
    
    public string FileName { get; }
    
    public long? ContentLength { get; }
    
    public PipeReader Content { get; }

    public async ValueTask DisposeAsync()
    {
        await Content.CompleteAsync();

        _response.Dispose();
    }
}
