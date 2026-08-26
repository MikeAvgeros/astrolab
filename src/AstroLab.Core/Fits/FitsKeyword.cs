namespace AstroLab.Core.Fits;

/// <summary>A single parsed 80-column FITS header card.</summary>
public readonly record struct FitsKeyword
{
    private FitsKeyword(string name, FitsValue value, string? comment)
    {
        Name = name;
        Value = value;
        Comment = comment;
    }

    /// <summary>The keyword name (e.g. <c>NAXIS1</c>), upper-cased and trimmed.</summary>
    public string Name { get; }

    /// <summary>The parsed value, or <see cref="FitsValue.None"/> for value-less cards.</summary>
    public FitsValue Value { get; }

    /// <summary>The free-text comment following <c>/</c>, if any.</summary>
    public string? Comment { get; }

    public static FitsKeyword Create(string name, FitsValue value, string? comment)
    {
        ArgumentNullException.ThrowIfNull(name);

        return new FitsKeyword(name, value, comment);
    }
}
