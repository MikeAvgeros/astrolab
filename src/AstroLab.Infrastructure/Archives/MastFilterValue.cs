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

    public static MastFilterValue FromRange(double min, double max) => new(null, min, max);
}
