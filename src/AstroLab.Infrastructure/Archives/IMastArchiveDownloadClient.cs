using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Archives;

public interface IMastArchiveDownloadClient
{
    Task<Result<ArchiveDownload>> DownloadAsync(MastProduct product, CancellationToken cancellationToken = default);
}
