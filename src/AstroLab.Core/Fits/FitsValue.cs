namespace AstroLab.Core.Fits;

/// <summary>
/// A parsed FITS header value. Exactly one of the typed accessors is meaningful, selected by
/// <see cref="Kind"/> — a small hand-rolled union, following the same shape as
/// <c>AstroLab.Core.Result.Result{TValue}</c>.
/// </summary>
public readonly struct FitsValue
{
    private readonly string? _text;
    private readonly long _integer;
    private readonly double _real;
    private readonly bool _logical;

    private FitsValue(FitsValueKind kind, string? text, long integer, double real, bool logical)
    {
        Kind = kind;
        _text = text;
        _integer = integer;
        _real = real;
        _logical = logical;
    }

    public FitsValueKind Kind { get; }

    public static readonly FitsValue None = new(FitsValueKind.None, null, 0, 0, false);

    public static FitsValue OfString(string value) => new(FitsValueKind.String, value, 0, 0, false);

    public static FitsValue OfInteger(long value) => new(FitsValueKind.Integer, null, value, value, false);

    public static FitsValue OfReal(double value) => new(FitsValueKind.Real, null, (long)value, value, false);

    public static FitsValue OfLogical(bool value) => new(FitsValueKind.Logical, null, value ? 1 : 0, value ? 1 : 0, value);

    public static FitsValue OfUndefined(string rawText) => new(FitsValueKind.Undefined, rawText, 0, 0, false);

    /// <summary>The literal string content when <see cref="Kind"/> is <see cref="FitsValueKind.String"/>.</summary>
    public string AsString => Kind == FitsValueKind.String
        ? _text!
        : throw new InvalidOperationException($"FitsValue is {Kind}, not String.");

    public long AsInteger => Kind == FitsValueKind.Integer
        ? _integer
        : throw new InvalidOperationException($"FitsValue is {Kind}, not Integer.");

    /// <summary>The numeric value, valid for both <see cref="FitsValueKind.Integer"/> and <see cref="FitsValueKind.Real"/>.</summary>
    public double AsReal => Kind is FitsValueKind.Real or FitsValueKind.Integer
        ? _real
        : throw new InvalidOperationException($"FitsValue is {Kind}, not Real or Integer.");

    public bool AsLogical => Kind == FitsValueKind.Logical
        ? _logical
        : throw new InvalidOperationException($"FitsValue is {Kind}, not Logical.");

    public TResult Match<TResult>(
        Func<TResult> onNone,
        Func<string, TResult> onString,
        Func<long, TResult> onInteger,
        Func<double, TResult> onReal,
        Func<bool, TResult> onLogical,
        Func<string, TResult> onUndefined) => Kind switch
        {
            FitsValueKind.None => onNone(),
            FitsValueKind.String => onString(_text!),
            FitsValueKind.Integer => onInteger(_integer),
            FitsValueKind.Real => onReal(_real),
            FitsValueKind.Logical => onLogical(_logical),
            FitsValueKind.Undefined => onUndefined(_text ?? string.Empty),
            _ => throw new InvalidOperationException($"Unhandled FitsValueKind: {Kind}"),
        };

    public override string ToString() => Match(
        onNone: () => string.Empty,
        onString: s => s,
        onInteger: i => i.ToString(),
        onReal: r => r.ToString("G17"),
        onLogical: b => b ? "T" : "F",
        onUndefined: s => s);
}
