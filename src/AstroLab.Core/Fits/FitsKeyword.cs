namespace AstroLab.Core.Fits;

/// <summary>A single parsed 80-column FITS header card.</summary>
/// <param name="Name">The keyword name (e.g. <c>NAXIS1</c>), upper-cased and trimmed.</param>
/// <param name="Value">The parsed value, or <see cref="FitsValue.None"/> for value-less cards.</param>
/// <param name="Comment">The free-text comment following <c>/</c>, if any.</param>
public readonly record struct FitsKeyword(string Name, FitsValue Value, string? Comment);

/// <summary>Static factory accompanying <see cref="FitsKeyword"/>.</summary>
public static class FitsKeywordFactory
{
    public static FitsKeyword Create(string name, FitsValue value, string? comment)
    {
        ArgumentNullException.ThrowIfNull(name);

        return new FitsKeyword(name, value, comment);
    }
}
