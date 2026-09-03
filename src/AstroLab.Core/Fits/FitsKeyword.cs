namespace AstroLab.Core.Fits;

public readonly record struct FitsKeyword
{
    private FitsKeyword(string name, FitsValue value, string? comment)
    {
        Name = name;
        Value = value;
        Comment = comment;
    }

    public string Name { get; }

    public FitsValue Value { get; }

    public string? Comment { get; }

    public static FitsKeyword Create(string name, FitsValue value, string? comment)
    {
        ArgumentNullException.ThrowIfNull(name);

        return new FitsKeyword(name, value, comment);
    }
}
