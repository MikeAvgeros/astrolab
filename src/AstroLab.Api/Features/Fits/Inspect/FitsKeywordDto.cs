namespace AstroLab.Api.Features.Fits.Inspect;

public sealed record FitsKeywordDto
{
    private FitsKeywordDto(string name, string value, string? comment)
    {
        Name = name;
        Value = value;
        Comment = comment;
    }

    public string Name { get; }

    public string Value { get; }

    public string? Comment { get; }

    public static FitsKeywordDto Create(string name, string value, string? comment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new FitsKeywordDto(name, value, comment);
    }
}
