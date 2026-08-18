using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Download;

public sealed record DownloadRequest(ArchiveSource Archive, string DatasetId);
