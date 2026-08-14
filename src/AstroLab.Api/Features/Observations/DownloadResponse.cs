using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Observations;

public sealed record DownloadResponse(string FileId, ArchiveSource Archive, long SizeBytes);
