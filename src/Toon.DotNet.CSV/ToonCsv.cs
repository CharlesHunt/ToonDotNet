using System.Globalization;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using ToonFormat;

namespace ToonFormat.Csv;

/// <summary>
/// Provides static methods to convert between CSV and TOON format.
/// </summary>
/// <remarks>
/// CSV data is treated as tabular — the first row provides column headers and all
/// subsequent rows become TOON data rows. When converting TOON to CSV the root
/// value must be an array of objects.
/// </remarks>
public static class ToonCsv
{
    // -------------------------------------------------------------------------
    // CSV → TOON
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts a CSV string to TOON tabular-array format.
    /// The first row of the CSV is used as column headers.
    /// </summary>
    /// <param name="csv">The CSV content to convert.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <returns>A TOON string representing the CSV data as a tabular array.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="csv"/> is null or empty.</exception>
    /// <example>
    /// <code>
    /// string toon = ToonCsv.FromCsv("id,name,role\n1,Alice,admin\n2,Bob,user");
    /// // [2]{id,name,role}:
    /// //   1,Alice,admin
    /// //   2,Bob,user
    /// </code>
    /// </example>
    public static string FromCsv(string csv, EncodeOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(csv);
        using var reader = new StringReader(csv);
        return Toon.Encode(ParseCsv(reader), options);
    }

    /// <summary>
    /// Reads CSV data from a <see cref="Stream"/> and converts it to TOON format.
    /// The stream is left open after the call.
    /// </summary>
    /// <param name="csvStream">The stream containing CSV data. Must be readable.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <param name="encoding">The text encoding to use. If null, <see cref="Encoding.UTF8"/> is used.</param>
    /// <returns>A TOON string representing the CSV data as a tabular array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="csvStream"/> is null.</exception>
    public static string FromCsv(Stream csvStream, EncodeOptions? options = null, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(csvStream);
        using var reader = new StreamReader(csvStream, encoding ?? Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
        return Toon.Encode(ParseCsv(reader), options);
    }

    /// <summary>
    /// Opens a CSV file and converts its contents to TOON format.
    /// </summary>
    /// <param name="csvPath">Path to the CSV file.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <returns>A TOON string representing the CSV file data as a tabular array.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="csvPath"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public static string FromCsvFile(string csvPath, EncodeOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(csvPath);
        if (!File.Exists(csvPath))
            throw new FileNotFoundException($"CSV file not found: {csvPath}", csvPath);

        using var reader = new StreamReader(csvPath, Encoding.UTF8);
        return Toon.Encode(ParseCsv(reader), options);
    }

    /// <summary>
    /// Reads a CSV file and saves the result as a TOON file.
    /// </summary>
    /// <param name="csvPath">Path to the source CSV file.</param>
    /// <param name="toonPath">Path to the destination TOON file to create or overwrite.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <exception cref="ArgumentException">Thrown when either path is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the CSV file does not exist.</exception>
    public static void SaveAsToon(string csvPath, string toonPath, EncodeOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(toonPath);
        File.WriteAllText(toonPath, FromCsvFile(csvPath, options));
    }

    /// <summary>
    /// Asynchronously opens a CSV file and converts its contents to TOON format.
    /// </summary>
    /// <param name="csvPath">Path to the CSV file.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A TOON string representing the CSV file data as a tabular array.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="csvPath"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public static async Task<string> FromCsvAsync(
        string csvPath,
        EncodeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(csvPath);
        if (!File.Exists(csvPath))
            throw new FileNotFoundException($"CSV file not found: {csvPath}", csvPath);

        var csv = await File.ReadAllTextAsync(csvPath, cancellationToken).ConfigureAwait(false);
        return FromCsv(csv, options);
    }

    /// <summary>
    /// Asynchronously reads a CSV file and saves the result as a TOON file.
    /// </summary>
    /// <param name="csvPath">Path to the source CSV file.</param>
    /// <param name="toonPath">Path to the destination TOON file to create or overwrite.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when either path is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the CSV file does not exist.</exception>
    public static async Task SaveAsToonAsync(
        string csvPath,
        string toonPath,
        EncodeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(toonPath);
        var toon = await FromCsvAsync(csvPath, options, cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(toonPath, toon, cancellationToken).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // TOON → CSV
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts a TOON string to CSV format.
    /// The TOON root value must be an array of objects (tabular array).
    /// </summary>
    /// <param name="toon">The TOON string to convert. Root value must be an array of objects.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <returns>A CSV string representation of the TOON data.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="toon"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the TOON root is not an array of objects.</exception>
    /// <example>
    /// <code>
    /// string csv = ToonCsv.ToCsv("[2]{id,name}:\n  1,Alice\n  2,Bob");
    /// // id,name
    /// // 1,Alice
    /// // 2,Bob
    /// </code>
    /// </example>
    public static string ToCsv(string toon, DecodeOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(toon);
        return WriteCsv(Toon.Decode(toon, options));
    }

    /// <summary>
    /// Converts a TOON string to CSV and writes it to <paramref name="outputStream"/>.
    /// The stream is left open after the call.
    /// </summary>
    /// <param name="toon">The TOON string to convert.</param>
    /// <param name="outputStream">The destination stream. Must be writable.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <param name="encoding">The text encoding to use. If null, UTF-8 without BOM is used.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="toon"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="outputStream"/> is null.</exception>
    public static void ToCsvStream(
        string toon,
        Stream outputStream,
        DecodeOptions? options = null,
        Encoding? encoding = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(toon);
        ArgumentNullException.ThrowIfNull(outputStream);
        var csv = ToCsv(toon, options);
        using var writer = new StreamWriter(outputStream,
            encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            bufferSize: 4096, leaveOpen: true);
        writer.Write(csv);
        writer.Flush();
    }

    /// <summary>
    /// Converts a TOON string to CSV and writes it to a file.
    /// </summary>
    /// <param name="toon">The TOON string to convert.</param>
    /// <param name="csvPath">Path to the destination CSV file to create or overwrite.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <exception cref="ArgumentException">Thrown when either argument is null or empty.</exception>
    public static void ToCsvFile(string toon, string csvPath, DecodeOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(csvPath);
        File.WriteAllText(csvPath, ToCsv(toon, options));
    }

    /// <summary>
    /// Asynchronously converts a TOON string to CSV and writes it to a file.
    /// </summary>
    /// <param name="toon">The TOON string to convert.</param>
    /// <param name="csvPath">Path to the destination CSV file to create or overwrite.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when either argument is null or empty.</exception>
    public static async Task ToCsvAsync(
        string toon,
        string csvPath,
        DecodeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(csvPath);
        await File.WriteAllTextAsync(csvPath, ToCsv(toon, options), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads a TOON file and converts it to a CSV file.
    /// </summary>
    /// <param name="toonPath">Path to the source TOON file.</param>
    /// <param name="csvPath">Path to the destination CSV file to create or overwrite.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <exception cref="ArgumentException">Thrown when either path is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the TOON file does not exist.</exception>
    public static void ConvertToonToCsv(string toonPath, string csvPath, DecodeOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(toonPath);
        ArgumentException.ThrowIfNullOrEmpty(csvPath);
        if (!File.Exists(toonPath))
            throw new FileNotFoundException($"TOON file not found: {toonPath}", toonPath);

        ToCsvFile(File.ReadAllText(toonPath), csvPath, options);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static List<Dictionary<string, object?>> ParseCsv(TextReader reader)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim,
        };

        using var csv = new CsvReader(reader, config);
        var rows = new List<Dictionary<string, object?>>();

        if (!csv.Read())
            return rows;

        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();

        while (csv.Read())
        {
            var dict = new Dictionary<string, object?>(headers.Length);
            foreach (var header in headers)
                dict[header] = CoerceValue(csv.GetField(header));
            rows.Add(dict);
        }

        return rows;
    }

    private static object? CoerceValue(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return null;

        if (bool.TryParse(raw, out bool b))
            return b;

        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
            return l;

        if (double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out double d))
            return d;

        return raw;
    }

    private static string WriteCsv(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException(
                $"TOON root must be an array to convert to CSV, but got {element.ValueKind}.");

        var rows = element.EnumerateArray().ToList();

        if (rows.Count == 0)
            return string.Empty;

        var firstObject = rows.FirstOrDefault(r => r.ValueKind == JsonValueKind.Object);
        if (firstObject.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException(
                "CSV conversion requires an array of objects with named fields.");

        var headers = firstObject.EnumerateObject().Select(p => p.Name).ToList();

        using var sw = new StringWriter();
        using var csv = new CsvWriter(sw, CultureInfo.InvariantCulture);

        foreach (var h in headers)
            csv.WriteField(h);
        csv.NextRecord();

        foreach (var row in rows)
        {
            if (row.ValueKind != JsonValueKind.Object)
                continue;

            foreach (var h in headers)
            {
                csv.WriteField(row.TryGetProperty(h, out var val)
                    ? ElementToString(val)
                    : string.Empty);
            }
            csv.NextRecord();
        }

        return sw.ToString();
    }

    private static string? ElementToString(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True   => "true",
        JsonValueKind.False  => "false",
        JsonValueKind.Null   => null,
        _                    => element.GetRawText(),
    };
}
