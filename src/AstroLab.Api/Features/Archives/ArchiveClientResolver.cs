using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives;

/// <summary>Resolves the upstream archive client (ESO or MAST) requested by a caller, shared by the Search and Download slices.</summary>
internal static class ArchiveClientResolver
{
    public static IArchiveClient Resolve(ArchiveSource archive, IEsoArchiveClient esoClient, IMastArchiveClient mastClient) => archive switch
    {
        ArchiveSource.Eso => esoClient,
        ArchiveSource.Mast => mastClient,
        _ => throw new ArgumentOutOfRangeException(nameof(archive), archive, "Unknown archive source."),
    };
}
