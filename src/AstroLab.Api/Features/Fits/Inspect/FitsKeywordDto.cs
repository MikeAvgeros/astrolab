namespace AstroLab.Api.Features.Fits.Inspect;

public sealed record FitsKeywordDto(string Name, string Value, string? Comment);

/// <summary>Static factory accompanying <see cref="FitsKeywordDto"/>. Validates arguments before constructing.</summary>
public static class FitsKeywordDtoFactory
{
    public static FitsKeywordDto Create(string name, string value, string? comment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new FitsKeywordDto(name, value, comment);
    }
}
