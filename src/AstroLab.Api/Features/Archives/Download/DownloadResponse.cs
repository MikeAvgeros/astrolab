using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Download;

public sealed record DownloadResponse(string FileId, ArchiveSource Archive, long SizeBytes);
