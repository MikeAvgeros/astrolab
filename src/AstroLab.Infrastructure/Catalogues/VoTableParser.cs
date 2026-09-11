using System.Xml.Linq;

namespace AstroLab.Infrastructure.Catalogues;

/// <summary>
/// Minimal reader for the IVOA VOTable XML format returned by a TAP sync query
/// (<c>FORMAT=votable</c>), the one output format every compliant TAP service is required to
/// support. Matches elements by local name so it tolerates the VOTable namespace differing across
/// service versions (v1.1 through v1.4 each use a distinct namespace URI).
/// </summary>
internal static class VoTableParser
{
    public static string? FindQueryErrorMessage(XDocument document)
    {
        var errorInfo = document
            .Descendants()
            .FirstOrDefault(e =>
                e.Name.LocalName == "INFO"
                && string.Equals(e.Attribute("name")?.Value, "QUERY_STATUS", StringComparison.OrdinalIgnoreCase)
                && string.Equals(e.Attribute("value")?.Value, "ERROR", StringComparison.OrdinalIgnoreCase));

        if (errorInfo is null)
        {
            return null;
        }

        var message = errorInfo.Value.Trim();

        return message.Length > 0 ? message : "VizieR reported a query error with no message.";
    }

    public static VoTableResult? Parse(XDocument document)
    {
        var table = document.Descendants().FirstOrDefault(e => e.Name.LocalName == "TABLE");

        if (table is null)
        {
            return null;
        }

        var fieldNames = table
            .Elements()
            .Where(e => e.Name.LocalName == "FIELD")
            .Select(field => field.Attribute("name")?.Value ?? string.Empty)
            .ToList();

        var rows = new List<IReadOnlyList<string?>>();

        var tableData = table.Descendants().FirstOrDefault(e => e.Name.LocalName == "TABLEDATA");

        if (tableData is not null)
        {
            foreach (var tr in tableData.Elements().Where(e => e.Name.LocalName == "TR"))
            {
                var cells = tr
                    .Elements()
                    .Where(e => e.Name.LocalName == "TD")
                    .Select(td => td.IsEmpty ? null : td.Value)
                    .ToList();

                rows.Add(cells);
            }
        }

        return new VoTableResult(fieldNames, rows);
    }
}
