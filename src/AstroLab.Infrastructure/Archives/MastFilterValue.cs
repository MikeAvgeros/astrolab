using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

[JsonConverter(typeof(MastFilterValueJsonConverter))]
internal readonly struct MastFilterValue
{
    private MastFilterValue(string? text, double? min, double? max)
    {
        Text = text;
        Min = min;
        Max = max;
    }

    public string? Text { get; }

    public double? Min { get; }

    public double? Max { get; }

    public static MastFilterValue FromText(string text) => new(text, null, null);

    public static MastFilterValue FromRange(double min, double max)
    {
        if (min > max)
        {
            throw new ArgumentException($"Range minimum ({min}) must not exceed maximum ({max}).", nameof(min));
        }

        return new MastFilterValue(null, min, max);
    }

    public static MastFilterValue FromMinBound(double min) => new(null, min, null);

    public static MastFilterValue FromMaxBound(double max) => new(null, null, max);
}
