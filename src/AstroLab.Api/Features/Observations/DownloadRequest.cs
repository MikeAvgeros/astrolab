using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Observations;

public sealed record DownloadRequest(ArchiveSource Archive, string DatasetId);
