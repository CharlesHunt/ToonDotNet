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
    // Multi-dataset: TOON → multiple CSVs
    // -------------------------------------------------------------------------
    //
    // CSV has no native multi-table concept, so — mirroring
    // Toon.DotNet.Excel's "one worksheet per top-level key" — the closest
    // equivalent here is "one CSV per top-level key," the same convention
    // database/BI export tools use when exporting multiple tables. These
    // methods require a root TOON *object* (one array per dataset); a root
    // array (a single, unnamed dataset) is out of scope for them — use
    // ToCsv/FromCsv for that, rather than inventing a default key name.

    /// <summary>
    /// Converts a multi-dataset TOON document (a root object whose
    /// top-level values are each an array of objects) to CSV, one entry
    /// per top-level key.
    /// </summary>
    /// <param name="toon">The TOON string to convert. Root value must be an object whose top-level values are each an array of objects.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <returns>A dictionary mapping each top-level key to its CSV content.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="toon"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the TOON root is not an object, or when a top-level value isn't an array of objects.</exception>
    /// <example>
    /// <code>
    /// var csvByName = ToonCsv.ToCsvDictionary(
    ///     "Sales[1]{id,amount}:\n  1,9.99\nCustomers[1]{id,name}:\n  1,Alice");
    /// // csvByName["Sales"]     -> "id,amount\r\n1,9.99\r\n"
    /// // csvByName["Customers"] -> "id,name\r\n1,Alice\r\n"
    /// </code>
    /// </example>
    public static Dictionary<string, string> ToCsvDictionary(string toon, DecodeOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(toon);
        return ToCsvDictionary(Toon.Decode(toon, options));
    }

    /// <summary>
    /// Converts a multi-dataset TOON document to CSV files, one file per
    /// top-level key, written to <paramref name="outputDirectory"/>
    /// (created if it doesn't already exist). Key names are sanitized for
    /// filesystem safety and de-duplicated with a numeric suffix if
    /// sanitization collides two different keys onto the same file name.
    /// </summary>
    /// <param name="toon">The TOON string to convert. Root value must be an object whose top-level values are each an array of objects.</param>
    /// <param name="outputDirectory">Directory to write the CSV files into. Created if it doesn't already exist.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <returns>A dictionary mapping each top-level key to the full path of the file written for it.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="toon"/> or <paramref name="outputDirectory"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the TOON root is not an object, or when a top-level value isn't an array of objects.</exception>
    /// <example>
    /// <code>
    /// var written = ToonCsv.ToCsvFiles(toon, "./export");
    /// // written["Sales"]     -> "./export/Sales.csv"
    /// // written["Customers"] -> "./export/Customers.csv"
    /// </code>
    /// </example>
    public static Dictionary<string, string> ToCsvFiles(string toon, string outputDirectory, DecodeOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(toon);
        ArgumentException.ThrowIfNullOrEmpty(outputDirectory);

        var csvByName = ToCsvDictionary(toon, options);
        Directory.CreateDirectory(outputDirectory);

        var writtenPaths = new Dictionary<string, string>();
        var usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, csv) in csvByName)
        {
            var fileName = UniqueCsvFileName(name, usedFileNames);
            var path = Path.Combine(outputDirectory, fileName);
            File.WriteAllText(path, csv);
            writtenPaths[name] = path;
        }

        return writtenPaths;
    }

    /// <summary>
    /// Asynchronously converts a multi-dataset TOON document to CSV files,
    /// one file per top-level key. See <see cref="ToCsvFiles"/>.
    /// </summary>
    /// <param name="toon">The TOON string to convert. Root value must be an object whose top-level values are each an array of objects.</param>
    /// <param name="outputDirectory">Directory to write the CSV files into. Created if it doesn't already exist.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A dictionary mapping each top-level key to the full path of the file written for it.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="toon"/> or <paramref name="outputDirectory"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the TOON root is not an object, or when a top-level value isn't an array of objects.</exception>
    public static async Task<Dictionary<string, string>> ToCsvFilesAsync(
        string toon,
        string outputDirectory,
        DecodeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(toon);
        ArgumentException.ThrowIfNullOrEmpty(outputDirectory);

        var csvByName = ToCsvDictionary(toon, options);
        Directory.CreateDirectory(outputDirectory);

        var writtenPaths = new Dictionary<string, string>();
        var usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, csv) in csvByName)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = UniqueCsvFileName(name, usedFileNames);
            var path = Path.Combine(outputDirectory, fileName);
            await File.WriteAllTextAsync(path, csv, cancellationToken).ConfigureAwait(false);
            writtenPaths[name] = path;
        }

        return writtenPaths;
    }

    // -------------------------------------------------------------------------
    // Multi-dataset: multiple CSVs → TOON (the reverse direction)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Combines multiple named CSV datasets into a single multi-dataset
    /// TOON document: a root object with one key per dataset name, each
    /// value the corresponding array of rows. The reverse of
    /// <see cref="ToCsvDictionary"/> — but note this always produces a
    /// root object, even for a single entry; it never unwraps to a bare
    /// root array (use <see cref="FromCsv(string, EncodeOptions?)"/> for
    /// that single-dataset case).
    /// </summary>
    /// <param name="csvByName">A mapping of dataset name to CSV content.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <returns>A TOON string representing a root object with one array per entry.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="csvByName"/> is null.</exception>
    /// <example>
    /// <code>
    /// var toon = ToonCsv.FromCsvDictionary(new Dictionary&lt;string, string&gt;
    /// {
    ///     ["Sales"] = "id,amount\n1,9.99",
    ///     ["Customers"] = "id,name\n1,Alice",
    /// });
    /// // Sales[1]{id,amount}:
    /// //   1,9.99
    /// // Customers[1]{id,name}:
    /// //   1,Alice
    /// </code>
    /// </example>
    public static string FromCsvDictionary(IReadOnlyDictionary<string, string> csvByName, EncodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(csvByName);

        var datasets = new Dictionary<string, object?>();
        foreach (var (name, csv) in csvByName)
        {
            using var reader = new StringReader(csv ?? string.Empty);
            datasets[name] = ParseCsv(reader);
        }

        return Toon.Encode(datasets, options);
    }

    /// <summary>
    /// Reads all CSV files in <paramref name="inputDirectory"/> matching
    /// <paramref name="searchPattern"/> and combines them into a single
    /// multi-dataset TOON document — a root object with one key per file
    /// (named after the file, extension stripped), each value the file's
    /// rows. Files are processed in ordinal file-path order for
    /// deterministic output. The reverse of <see cref="ToCsvFiles"/>.
    /// </summary>
    /// <param name="inputDirectory">Directory to read CSV files from.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <param name="searchPattern">Glob pattern for matching CSV files. Defaults to <c>"*.csv"</c>.</param>
    /// <returns>A TOON string representing a root object with one array per file. An empty object (<c>{}</c>) if no files match.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="inputDirectory"/> is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
    public static string FromCsvFiles(string inputDirectory, EncodeOptions? options = null, string searchPattern = "*.csv")
    {
        ArgumentException.ThrowIfNullOrEmpty(inputDirectory);
        if (!Directory.Exists(inputDirectory))
            throw new DirectoryNotFoundException($"Directory not found: {inputDirectory}");

        var datasets = new Dictionary<string, object?>();
        foreach (var path in Directory.GetFiles(inputDirectory, searchPattern).OrderBy(p => p, StringComparer.Ordinal))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            using var reader = new StreamReader(path, Encoding.UTF8);
            datasets[name] = ParseCsv(reader);
        }

        return Toon.Encode(datasets, options);
    }

    /// <summary>
    /// Asynchronously reads all CSV files in a directory and combines
    /// them into a single multi-dataset TOON document. See
    /// <see cref="FromCsvFiles"/>.
    /// </summary>
    /// <param name="inputDirectory">Directory to read CSV files from.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <param name="searchPattern">Glob pattern for matching CSV files. Defaults to <c>"*.csv"</c>.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A TOON string representing a root object with one array per file. An empty object (<c>{}</c>) if no files match.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="inputDirectory"/> is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
    public static async Task<string> FromCsvFilesAsync(
        string inputDirectory,
        EncodeOptions? options = null,
        string searchPattern = "*.csv",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(inputDirectory);
        if (!Directory.Exists(inputDirectory))
            throw new DirectoryNotFoundException($"Directory not found: {inputDirectory}");

        var datasets = new Dictionary<string, object?>();
        foreach (var path in Directory.GetFiles(inputDirectory, searchPattern).OrderBy(p => p, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = Path.GetFileNameWithoutExtension(path);
            var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            using var reader = new StringReader(content);
            datasets[name] = ParseCsv(reader);
        }

        return Toon.Encode(datasets, options);
    }

    /// <summary>
    /// Reads all CSV files in a directory and saves the combined
    /// multi-dataset TOON document to a file. Convenience wrapper over
    /// <see cref="FromCsvFiles"/> plus writing the result to disk.
    /// </summary>
    /// <param name="inputDirectory">Directory to read CSV files from.</param>
    /// <param name="toonPath">Path to the destination .toon file to create or overwrite.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <param name="searchPattern">Glob pattern for matching CSV files. Defaults to <c>"*.csv"</c>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="inputDirectory"/> or <paramref name="toonPath"/> is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
    public static void SaveCsvFilesAsToon(string inputDirectory, string toonPath, EncodeOptions? options = null, string searchPattern = "*.csv")
    {
        ArgumentException.ThrowIfNullOrEmpty(toonPath);
        File.WriteAllText(toonPath, FromCsvFiles(inputDirectory, options, searchPattern));
    }

    /// <summary>
    /// Asynchronously reads all CSV files in a directory and saves the
    /// combined multi-dataset TOON document to a file. See
    /// <see cref="SaveCsvFilesAsToon"/>.
    /// </summary>
    /// <param name="inputDirectory">Directory to read CSV files from.</param>
    /// <param name="toonPath">Path to the destination .toon file to create or overwrite.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <param name="searchPattern">Glob pattern for matching CSV files. Defaults to <c>"*.csv"</c>.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="inputDirectory"/> or <paramref name="toonPath"/> is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
    public static async Task SaveCsvFilesAsToonAsync(
        string inputDirectory,
        string toonPath,
        EncodeOptions? options = null,
        string searchPattern = "*.csv",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(toonPath);
        var toon = await FromCsvFilesAsync(inputDirectory, options, searchPattern, cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(toonPath, toon, cancellationToken).ConfigureAwait(false);
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

        // TOON spec §4: a leading zero followed by another digit (e.g.
        // "05", "-0042") makes the token a string in TOON's number
        // grammar, not a number. Coercing it to long/double here — before
        // the value ever reaches the encoder — would silently drop the
        // leading zero (e.g. a zip code becoming a smaller integer).
        if (!HasForbiddenLeadingZero(raw))
        {
            if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                return l;

            if (double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture, out double d))
                return d;
        }

        return raw;
    }

    /// <summary>
    /// Checks whether a token has a leading zero followed by another digit
    /// (e.g. "05", "-0001"), which spec §4 excludes from the number
    /// grammar. Mirrors ToonFormat.Shared.LiteralUtils's internal check of
    /// the same name; duplicated here since that one isn't visible across
    /// the package boundary.
    /// </summary>
    private static bool HasForbiddenLeadingZero(string value)
    {
        int start = value.Length > 0 && value[0] == '-' ? 1 : 0;

        if (value.Length <= start + 1 || value[start] != '0')
            return false;

        char next = value[start + 1];
        return next >= '0' && next <= '9';
    }

    /// <summary>
    /// Converts a decoded multi-dataset TOON object into one CSV string
    /// per top-level key, reusing <see cref="WriteCsv"/>'s array-of-objects
    /// validation and error messages for each dataset.
    /// </summary>
    private static Dictionary<string, string> ToCsvDictionary(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException(
                $"TOON root must be an object to convert to multiple CSV datasets (one top-level key per dataset), but got {element.ValueKind}. Use ToCsv for a single root array.");

        var result = new Dictionary<string, string>();
        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException(
                    $"Top-level key '{property.Name}' must be an array of objects to convert to CSV, but got {property.Value.ValueKind}.");

            result[property.Name] = WriteCsv(property.Value);
        }

        return result;
    }

    /// <summary>
    /// Builds a unique, filesystem-safe CSV file name (including
    /// extension) for a dataset key, tracking names already used in
    /// <paramref name="usedFileNames"/> so two keys that sanitize to the
    /// same base name don't collide (appends a numeric suffix instead).
    /// </summary>
    private static string UniqueCsvFileName(string key, HashSet<string> usedFileNames)
    {
        var baseName = SanitizeFileNameKey(key);
        var candidate = $"{baseName}.csv";
        if (usedFileNames.Add(candidate))
            return candidate;

        for (int n = 2; n <= 9999; n++)
        {
            candidate = $"{baseName} ({n}).csv";
            if (usedFileNames.Add(candidate))
                return candidate;
        }

        // Fallback: let File.WriteAllText throw for the truly unusual case
        // of 9999 colliding keys.
        return candidate;
    }

    /// <summary>
    /// Strips characters invalid in file names on the current platform
    /// and trims to a practical length, falling back to a generic name if
    /// nothing usable remains.
    /// </summary>
    private static string SanitizeFileNameKey(string key)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(key.Where(c => !invalid.Contains(c)).ToArray()).Trim();

        if (sanitized.Length > 100)
            sanitized = sanitized[..100];

        return string.IsNullOrWhiteSpace(sanitized) ? "table" : sanitized;
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
        JsonValueKind.Number => FormatNumber(element),
        JsonValueKind.True   => "true",
        JsonValueKind.False  => "false",
        JsonValueKind.Null   => null,
        _                    => element.GetRawText(),
    };

    /// <summary>
    /// Formats a decoded number cleanly for CSV output. element.GetRawText()
    /// would instead reflect the decoder's internal storage of the value
    /// (currently G17-formatted for doubles), producing needlessly verbose
    /// text like "9.9900000000000002" for a clean source value of "9.99".
    /// </summary>
    private static string FormatNumber(JsonElement element)
    {
        if (element.TryGetInt64(out long l))
            return l.ToString(CultureInfo.InvariantCulture);

        return element.GetDouble().ToString(CultureInfo.InvariantCulture);
    }
}
