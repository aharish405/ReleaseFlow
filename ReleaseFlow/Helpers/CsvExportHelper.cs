using System.Text;
using System.Reflection;

namespace ReleaseFlow.Helpers;

/// <summary>
/// Helper class for exporting data to CSV format
/// </summary>
public static class CsvExportHelper
{
    /// <summary>
    /// Converts a list of objects to CSV format
    /// </summary>
    public static string ToCsv<T>(IEnumerable<T> data, params string[] propertiesToInclude)
    {
        var sb = new StringBuilder();
        var type = typeof(T);

        // Get properties to export
        var properties = propertiesToInclude.Length > 0
            ? type.GetProperties().Where(p => propertiesToInclude.Any(name => name == p.Name)).ToArray()
            : type.GetProperties().Where(p => p.CanRead && IsSimpleType(p.PropertyType)).ToArray();

        // Write header
        sb.AppendLine(string.Join(",", properties.Select(p => EscapeCsvValue(p.Name))));

        // Write data rows
        foreach (var item in data)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item);
                return EscapeCsvValue(value?.ToString() ?? string.Empty);
            });
            sb.AppendLine(string.Join(",", values));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes a value for CSV format
    /// </summary>
    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // If value contains comma, quote, or newline, wrap in quotes and escape quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// Checks if a type is a simple type (string, number, bool, DateTime, etc.)
    /// </summary>
    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || (Nullable.GetUnderlyingType(type) != null && IsSimpleType(Nullable.GetUnderlyingType(type)!));
    }

    /// <summary>
    /// Creates a FileContentResult for CSV download
    /// </summary>
    public static byte[] GetCsvBytes(string csvContent)
    {
        // Add UTF-8 BOM for Excel compatibility
        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(csvContent);
        var result = new byte[preamble.Length + content.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(content, 0, result, preamble.Length, content.Length);
        return result;
    }

    /// <summary>
    /// Generates a filename with timestamp
    /// </summary>
    public static string GetTimestampedFilename(string baseName)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"{baseName}_{timestamp}.csv";
    }
}
