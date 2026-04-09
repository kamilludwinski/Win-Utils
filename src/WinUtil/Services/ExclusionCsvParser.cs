using System.Linq;
using System.Text;

namespace WinUtil.Services;

/// <summary>Parses two-column CSV rows (name, description) with RFC-4180–style quoted fields.</summary>
internal static class ExclusionCsvParser
{
    /// <summary>Reads the name column from a CSV file. Skips a header row when it is name,description.</summary>
    public static IReadOnlyList<string> LoadNames(string[] lines)
    {
        var names = new List<string>();
        var sawHeader = false;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var fields = ParseLine(line);
            if (fields.Count == 0)
                continue;

            if (!sawHeader && IsHeaderRow(fields))
            {
                sawHeader = true;
                continue;
            }

            sawHeader = true;
            var name = fields[0].Trim();
            if (name.Length > 0)
                names.Add(name);
        }

        return names;
    }

    /// <summary>Reads name + description columns from CSV rows.</summary>
    public static List<ExclusionEntry> LoadExclusionEntries(string[] lines)
    {
        var list = new List<ExclusionEntry>();
        var sawHeader = false;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var fields = ParseLine(line);
            if (fields.Count == 0)
                continue;

            if (!sawHeader && IsHeaderRow(fields))
            {
                sawHeader = true;
                continue;
            }

            sawHeader = true;
            var name = fields[0].Trim();
            if (name.Length == 0)
                continue;

            var description = fields.Count > 1
                ? string.Join(",", fields.Skip(1)).Trim()
                : string.Empty;

            list.Add(new ExclusionEntry { Name = name, Description = description });
        }

        return list;
    }

    /// <summary>Legacy: one process name per line (comments with #).</summary>
    public static IReadOnlyList<string> LoadLegacyCommentedLines(string[] lines)
    {
        var names = new List<string>();
        foreach (var line in lines)
        {
            var t = line.Trim();
            if (t.Length == 0 || t.StartsWith('#'))
                continue;
            names.Add(t);
        }

        return names;
    }

    private static bool IsHeaderRow(IReadOnlyList<string> fields) =>
        fields.Count >= 2
        && fields[0].Trim().Equals("name", StringComparison.OrdinalIgnoreCase)
        && fields[1].Trim().Equals("description", StringComparison.OrdinalIgnoreCase);

    /// <summary>Parses a single CSV line into fields (handles quotes and doubled quotes).</summary>
    public static List<string> ParseLine(string line)
    {
        var fields = new List<string>();
        var i = 0;
        var len = line.Length;

        while (i < len)
        {
            if (line[i] == '"')
            {
                i++;
                var sb = new StringBuilder();
                while (i < len)
                {
                    if (line[i] == '"')
                    {
                        if (i + 1 < len && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i += 2;
                            continue;
                        }

                        i++;
                        break;
                    }

                    sb.Append(line[i]);
                    i++;
                }

                fields.Add(sb.ToString());
            }
            else
            {
                var start = i;
                while (i < len && line[i] != ',')
                    i++;

                fields.Add(line.AsSpan(start, i - start).Trim().ToString());
            }

            if (i < len && line[i] == ',')
            {
                i++;
                if (i == len)
                    fields.Add(string.Empty);
            }
        }

        return fields;
    }

    /// <summary>Escapes a field for CSV output when needed.</summary>
    public static string EscapeField(string value)
    {
        if (value.Length == 0)
            return string.Empty;

        var mustQuote = false;
        foreach (var c in value)
        {
            if (c is '"' or ',' or '\r' or '\n')
            {
                mustQuote = true;
                break;
            }
        }

        if (!mustQuote)
            return value;

        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    /// <summary>Builds a two-column CSV line.</summary>
    public static string FormatRow(string name, string description) =>
        $"{EscapeField(name)},{EscapeField(description)}";

    /// <summary>UTF-8 BOM + header line for new exclusion files.</summary>
    public static byte[] Utf8BomHeader(string headerLine)
    {
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(headerLine + "\r\n");
        var result = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
        return result;
    }
}
