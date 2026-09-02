using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Archives;

public interface IEsoArchiveDownloadClient
{
    Task<Result<ArchiveDownload>> DownloadAsync(EsoProduct product, CancellationToken cancellationToken = default);
}
