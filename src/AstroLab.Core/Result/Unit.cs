namespace AstroLab.Core.Result;

/// <summary>A type with exactly one value, used as the <c>TValue</c> of a <see cref="Result{TValue}"/> that
/// carries no data — i.e. an operation that either succeeds or fails with an <see cref="Error"/>.</summary>
public readonly struct Unit
{
    public static readonly Unit Value = default;

    public override string ToString() => "()";
}
