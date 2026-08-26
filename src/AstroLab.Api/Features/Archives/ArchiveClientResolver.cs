using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives;

internal static class ArchiveClientResolver
{
    public static IArchiveClient Resolve(ArchiveSource archive, IEsoArchiveClient esoClient, IMastArchiveClient mastClient) => archive switch
    {
        ArchiveSource.Eso => esoClient,
        ArchiveSource.Mast => mastClient,
        _ => throw new ArgumentOutOfRangeException(nameof(archive), archive, "Unknown archive source."),
    };
}
