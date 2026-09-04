namespace AstroLab.Infrastructure.Archives;

public sealed record MastTarget
{
    private MastTarget(string name, double rightAscension, double declination)
    {
        Name = name;
        RightAscension = rightAscension;
        Declination = declination;
    }

    public string Name { get; }

    public double RightAscension { get; }

    public double Declination { get; }

    public static MastTarget Create(string name, double rightAscension, double declination)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new MastTarget(name, rightAscension, declination);
    }
}
