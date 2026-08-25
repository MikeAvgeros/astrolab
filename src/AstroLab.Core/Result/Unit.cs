namespace AstroLab.Core.Result;

public readonly struct Unit
{
    public static readonly Unit Value = default;

    public override string ToString() => "()";
}
