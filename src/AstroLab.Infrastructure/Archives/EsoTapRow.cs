using System.Globalization;
using System.Text.Json;

namespace AstroLab.Infrastructure.Archives;

internal sealed class EsoTapRow
{
    private readonly IReadOnlyDictionary<string, int> _columnIndex;
    private readonly List<object> _values;

    public EsoTapRow(IReadOnlyDictionary<string, int> columnIndex, List<object> values)
    {
        _columnIndex = columnIndex;
        _values = values;
    }

    public string? GetString(string columnName)
    {
        return GetRaw(columnName) switch
        {
            null => null,
            JsonElement { ValueKind: JsonValueKind.Null } => null,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            JsonElement element => element.ToString(),
            var raw => raw.ToString(),
        };
    }

    public double? GetDouble(string columnName)
    {
        return GetRaw(columnName) switch
        {
            null => null,
            JsonElement { ValueKind: JsonValueKind.Null } => null,
            JsonElement { ValueKind: JsonValueKind.Number } element => element.GetDouble(),
            JsonElement { ValueKind: JsonValueKind.String } element when
                double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
            var raw when double.TryParse(raw.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }

    public int? GetInt(string columnName) => (int?)GetDouble(columnName);

    public long? GetLong(string columnName) => (long?)GetDouble(columnName);

    private object? GetRaw(string columnName)
    {
        if (!_columnIndex.TryGetValue(columnName, out var index) || index >= _values.Count)
        {
            return null;
        }

        return _values[index];
    }
}
