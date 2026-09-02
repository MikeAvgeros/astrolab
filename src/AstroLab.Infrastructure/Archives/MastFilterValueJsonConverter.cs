using System.Text.Json;
using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

internal sealed class MastFilterValueJsonConverter : JsonConverter<MastFilterValue>
{
    public override MastFilterValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException($"{nameof(MastFilterValue)} is write-only and cannot be deserialized.");

    public override void Write(Utf8JsonWriter writer, MastFilterValue value, JsonSerializerOptions options)
    {
        if (value.Text is not null)
        {
            writer.WriteStringValue(value.Text);
            return;
        }

        writer.WriteStartObject();

        if (value.Min is { } min)
        {
            writer.WriteNumber("min", min);
        }

        if (value.Max is { } max)
        {
            writer.WriteNumber("max", max);
        }

        writer.WriteEndObject();
    }
}
