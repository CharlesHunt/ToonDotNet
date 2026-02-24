using ToonFormat;

namespace ToonFormat.Csv;

/// <summary>
/// Extension methods that add TOON encoding and decoding capabilities directly to
/// CSV strings and streams.
/// </summary>
public static class CsvToonExtensions
{
    /// <summary>
    /// Converts this CSV string to TOON tabular-array format.
    /// The first row of the CSV is used as column headers.
    /// </summary>
    /// <param name="csv">The CSV string to convert.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <returns>A TOON string representing the CSV data.</returns>
    /// <example>
    /// <code>
    /// string toon = "id,name\n1,Alice\n2,Bob".CsvToToon();
    /// </code>
    /// </example>
    public static string CsvToToon(this string csv, EncodeOptions? options = null)
        => ToonCsv.FromCsv(csv, options);

    /// <summary>
    /// Converts this TOON string to CSV format.
    /// The TOON root value must be an array of objects.
    /// </summary>
    /// <param name="toon">The TOON string to convert.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <returns>A CSV string representation of the TOON data.</returns>
    /// <example>
    /// <code>
    /// string csv = "[2]{id,name}:\n  1,Alice\n  2,Bob".ToonToCsv();
    /// </code>
    /// </example>
    public static string ToonToCsv(this string toon, DecodeOptions? options = null)
        => ToonCsv.ToCsv(toon, options);
}
