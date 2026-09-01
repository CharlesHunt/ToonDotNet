using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;

namespace ToonFormat.Excel;

/// <summary>
/// Provides static methods to encode Excel workbooks/worksheets to TOON format
/// and decode TOON format back into Excel workbooks.
/// </summary>
/// <remarks>
/// <para>
/// Encoding a single <see cref="IXLWorksheet"/> produces a root TOON array where the
/// first worksheet row is used as column headers.
/// </para>
/// <para>
/// Encoding an <see cref="IXLWorkbook"/> produces a root TOON object where each
/// top-level key is a sheet name containing the sheet's tabular data.
/// </para>
/// <para>
/// Decoding a TOON string that has a root object creates one worksheet per key.
/// Decoding a root array creates a single worksheet named <c>Sheet1</c>.
/// </para>
/// </remarks>
public static class ToonExcel
{
    // -------------------------------------------------------------------------
    // Encoding  (Excel → TOON)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Encodes a single worksheet to TOON format.
    /// The first row is treated as column headers; subsequent rows become data rows.
    /// The result is a root TOON array.
    /// </summary>
    /// <param name="worksheet">The worksheet to encode.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <returns>A TOON format string representing the worksheet data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="worksheet"/> is null.</exception>
    /// <example>
    /// <code>
    /// using var workbook = new XLWorkbook("data.xlsx");
    /// string toon = workbook.Worksheet("Sales").ToToon();
    /// // [3]{id,product,amount}:
    /// //   1,Widget,9.99
    /// //   2,Gadget,19.99
    /// //   3,Doohickey,4.99
    /// </code>
    /// </example>
    public static string Encode(IXLWorksheet worksheet, EncodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(worksheet);
        var rows = WorksheetToRowList(worksheet);
        return Toon.Encode(rows, options);
    }

    /// <summary>
    /// Encodes all worksheets in a workbook to TOON format.
    /// Each sheet becomes a named property in the resulting TOON object, keyed by sheet name.
    /// </summary>
    /// <param name="workbook">The workbook to encode.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <returns>A TOON format string where each top-level key is a sheet name.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="workbook"/> is null.</exception>
    /// <example>
    /// <code>
    /// using var workbook = new XLWorkbook("report.xlsx");
    /// string toon = ToonExcel.Encode(workbook);
    /// // Sales[3]{id,product,amount}:
    /// //   1,Widget,9.99
    /// //   ...
    /// // Customers[2]{id,name}:
    /// //   ...
    /// </code>
    /// </example>
    public static string Encode(IXLWorkbook workbook, EncodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        var sheetsData = new Dictionary<string, object?>();
        foreach (var worksheet in workbook.Worksheets)
            sheetsData[worksheet.Name] = WorksheetToRowList(worksheet);
        return Toon.Encode(sheetsData, options);
    }

    /// <summary>
    /// Opens an Excel (.xlsx) file and encodes its contents to TOON format.
    /// Each sheet becomes a named property in the resulting TOON object.
    /// </summary>
    /// <param name="excelFilePath">Path to the .xlsx file.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <returns>A TOON format string representing the workbook.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="excelFilePath"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public static string EncodeFile(string excelFilePath, EncodeOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(excelFilePath);
        if (!File.Exists(excelFilePath))
            throw new FileNotFoundException($"Excel file not found: {excelFilePath}", excelFilePath);

        using var workbook = new XLWorkbook(excelFilePath);
        return Encode(workbook, options);
    }

    /// <summary>
    /// Reads an Excel file and saves its contents as a TOON file.
    /// </summary>
    /// <param name="excelFilePath">Path to the source .xlsx file.</param>
    /// <param name="toonFilePath">Path to the destination .toon file to create or overwrite.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <exception cref="ArgumentException">Thrown when either path is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the Excel file does not exist.</exception>
    public static void SaveAsToon(string excelFilePath, string toonFilePath, EncodeOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(toonFilePath);
        var toon = EncodeFile(excelFilePath, options);
        File.WriteAllText(toonFilePath, toon);
    }

    // -------------------------------------------------------------------------
    // Sheet selection
    // -------------------------------------------------------------------------

    /// <summary>
    /// Encodes a single named worksheet from a workbook to TOON format.
    /// The result is a root TOON array (identical to calling
    /// <see cref="Encode(IXLWorksheet, EncodeOptions?)"/> on the named sheet directly).
    /// </summary>
    /// <param name="workbook">The workbook to read from.</param>
    /// <param name="sheetName">The name of the worksheet to encode.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <returns>A TOON format string representing the named worksheet's data as a root array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="workbook"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sheetName"/> is null or empty, or when no sheet with that name exists.
    /// </exception>
    /// <example>
    /// <code>
    /// using var workbook = new XLWorkbook("report.xlsx");
    /// string toon = ToonExcel.Encode(workbook, "Sales");
    /// // [3]{id,product,amount}:
    /// //   1,Widget,9.99
    /// </code>
    /// </example>
    public static string Encode(IXLWorkbook workbook, string sheetName, EncodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        ArgumentException.ThrowIfNullOrEmpty(sheetName);

        var worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == sheetName)
            ?? throw new ArgumentException($"Sheet '{sheetName}' not found in workbook.", nameof(sheetName));

        return Encode(worksheet, options);
    }

    /// <summary>
    /// Encodes a subset of named worksheets from a workbook to TOON format.
    /// Each selected sheet becomes a named property in the resulting TOON object, keyed by sheet name.
    /// </summary>
    /// <param name="workbook">The workbook to read from.</param>
    /// <param name="sheetNames">The names of the worksheets to include. Must not be empty.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <returns>A TOON format string where each top-level key is one of the selected sheet names.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="workbook"/> or <paramref name="sheetNames"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sheetNames"/> is empty, or when any named sheet does not exist.
    /// </exception>
    /// <example>
    /// <code>
    /// using var workbook = new XLWorkbook("report.xlsx");
    /// string toon = ToonExcel.Encode(workbook, new[] { "Sales", "Costs" });
    /// // Sales[3]{id,product,amount}:
    /// //   ...
    /// // Costs[2]{id,amount}:
    /// //   ...
    /// </code>
    /// </example>
    public static string Encode(IXLWorkbook workbook, IEnumerable<string> sheetNames, EncodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        ArgumentNullException.ThrowIfNull(sheetNames);

        var names = sheetNames.ToList();
        if (names.Count == 0)
            throw new ArgumentException("At least one sheet name must be specified.", nameof(sheetNames));

        var sheetsData = new Dictionary<string, object?>();
        foreach (var name in names)
        {
            var worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == name)
                ?? throw new ArgumentException($"Sheet '{name}' not found in workbook.", nameof(sheetNames));
            sheetsData[name] = WorksheetToRowList(worksheet);
        }

        return Toon.Encode(sheetsData, options);
    }

    // -------------------------------------------------------------------------
    // Decoding  (TOON → Excel)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Decodes a TOON string into a new <see cref="XLWorkbook"/>.
    /// <list type="bullet">
    ///   <item>Root TOON object → one worksheet per key, named after the key.</item>
    ///   <item>Root TOON array  → single worksheet named <c>Sheet1</c>.</item>
    /// </list>
    /// </summary>
    /// <param name="toonString">The TOON format string to decode.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <returns>
    /// A new <see cref="XLWorkbook"/> populated from the TOON data.
    /// The caller is responsible for disposing the returned workbook.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="toonString"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the TOON string contains invalid syntax.</exception>
    public static XLWorkbook Decode(string toonString, DecodeOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(toonString);
        var element = Toon.Decode(toonString, options);
        return JsonElementToWorkbook(element);
    }

    /// <summary>
    /// Reads a TOON file and returns its contents as a new <see cref="XLWorkbook"/>.
    /// </summary>
    /// <param name="toonFilePath">Path to the .toon file.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <returns>
    /// A new <see cref="XLWorkbook"/> populated from the TOON file.
    /// The caller is responsible for disposing the returned workbook.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="toonFilePath"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public static XLWorkbook LoadToonFile(string toonFilePath, DecodeOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(toonFilePath);
        if (!File.Exists(toonFilePath))
            throw new FileNotFoundException($"TOON file not found: {toonFilePath}", toonFilePath);

        return Decode(File.ReadAllText(toonFilePath), options);
    }

    /// <summary>
    /// Decodes a TOON string and saves the result as an Excel (.xlsx) file.
    /// </summary>
    /// <param name="toonString">The TOON format string to decode.</param>
    /// <param name="excelFilePath">Path to the destination .xlsx file to create or overwrite.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <exception cref="ArgumentException">Thrown when either argument is null or empty.</exception>
    public static void SaveAsExcel(string toonString, string excelFilePath, DecodeOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(excelFilePath);
        using var workbook = Decode(toonString, options);
        workbook.SaveAs(excelFilePath);
    }

    /// <summary>
    /// Reads a TOON file and converts it to an Excel (.xlsx) file.
    /// </summary>
    /// <param name="toonFilePath">Path to the source .toon file.</param>
    /// <param name="excelFilePath">Path to the destination .xlsx file to create or overwrite.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <exception cref="ArgumentException">Thrown when either path is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the TOON file does not exist.</exception>
    public static void ConvertToonToExcel(string toonFilePath, string excelFilePath, DecodeOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(excelFilePath);
        using var workbook = LoadToonFile(toonFilePath, options);
        workbook.SaveAs(excelFilePath);
    }

    // -------------------------------------------------------------------------
    // Async  (Excel → TOON and TOON → Excel)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Asynchronously encodes a single worksheet to TOON format.
    /// Because ClosedXML worksheet access is synchronous this method completes
    /// synchronously and returns an already-completed task.
    /// </summary>
    /// <param name="worksheet">The worksheet to encode.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <param name="cancellationToken">Token to cancel the operation before it starts.</param>
    /// <returns>A task whose result is the TOON format string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="worksheet"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is already cancelled.</exception>
    public static Task<string> EncodeAsync(IXLWorksheet worksheet, EncodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(worksheet);
        return Task.FromResult(Encode(worksheet, options));
    }

    /// <summary>
    /// Asynchronously encodes all worksheets in a workbook to TOON format.
    /// Because ClosedXML workbook access is synchronous this method completes
    /// synchronously and returns an already-completed task.
    /// </summary>
    /// <param name="workbook">The workbook to encode.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <param name="cancellationToken">Token to cancel the operation before it starts.</param>
    /// <returns>A task whose result is the TOON format string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="workbook"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is already cancelled.</exception>
    public static Task<string> EncodeAsync(IXLWorkbook workbook, EncodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(workbook);
        return Task.FromResult(Encode(workbook, options));
    }

    /// <summary>
    /// Asynchronously opens an Excel (.xlsx) file and encodes its contents to TOON format.
    /// The file is opened on a background thread to avoid blocking the caller.
    /// </summary>
    /// <param name="excelFilePath">Path to the .xlsx file.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <param name="cancellationToken">Token to cancel the background operation.</param>
    /// <returns>A task whose result is the TOON format string.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="excelFilePath"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public static Task<string> EncodeFileAsync(string excelFilePath, EncodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(excelFilePath);
        if (!File.Exists(excelFilePath))
            throw new FileNotFoundException($"Excel file not found: {excelFilePath}", excelFilePath);

        return Task.Run(() => EncodeFile(excelFilePath, options), cancellationToken);
    }

    /// <summary>
    /// Asynchronously reads an Excel file and saves its contents as a TOON file.
    /// </summary>
    /// <param name="excelFilePath">Path to the source .xlsx file.</param>
    /// <param name="toonFilePath">Path to the destination .toon file to create or overwrite.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when either path is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the Excel file does not exist.</exception>
    public static async Task SaveAsToonAsync(string excelFilePath, string toonFilePath, EncodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(toonFilePath);
        var toon = await EncodeFileAsync(excelFilePath, options, cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(toonFilePath, toon, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously decodes a TOON string into a new <see cref="XLWorkbook"/>.
    /// Because TOON decoding and workbook construction are synchronous this method
    /// completes synchronously and returns an already-completed task.
    /// The caller is responsible for disposing the returned workbook.
    /// </summary>
    /// <param name="toonString">The TOON format string to decode.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <param name="cancellationToken">Token to cancel the operation before it starts.</param>
    /// <returns>A task whose result is a new <see cref="XLWorkbook"/> populated from the TOON data.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="toonString"/> is null or empty.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is already cancelled.</exception>
    public static Task<XLWorkbook> DecodeAsync(string toonString, DecodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Decode(toonString, options));
    }

    /// <summary>
    /// Asynchronously decodes a TOON string and saves the result as an Excel (.xlsx) file.
    /// The workbook is saved on a background thread to avoid blocking the caller.
    /// </summary>
    /// <param name="toonString">The TOON format string to decode.</param>
    /// <param name="excelFilePath">Path to the destination .xlsx file to create or overwrite.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <param name="cancellationToken">Token to cancel the background operation.</param>
    /// <exception cref="ArgumentException">Thrown when either argument is null or empty.</exception>
    public static Task SaveAsExcelAsync(string toonString, string excelFilePath, DecodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(excelFilePath);
        return Task.Run(() => SaveAsExcel(toonString, excelFilePath, options), cancellationToken);
    }

    /// <summary>
    /// Asynchronously reads a TOON file and converts it to an Excel (.xlsx) file.
    /// The TOON file is read asynchronously; workbook construction and save run on a background thread.
    /// </summary>
    /// <param name="toonFilePath">Path to the source .toon file.</param>
    /// <param name="excelFilePath">Path to the destination .xlsx file to create or overwrite.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when either path is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the TOON file does not exist.</exception>
    public static async Task ConvertToonToExcelAsync(string toonFilePath, string excelFilePath, DecodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(toonFilePath);
        ArgumentException.ThrowIfNullOrEmpty(excelFilePath);
        if (!File.Exists(toonFilePath))
            throw new FileNotFoundException($"TOON file not found: {toonFilePath}", toonFilePath);

        var toon = await File.ReadAllTextAsync(toonFilePath, cancellationToken).ConfigureAwait(false);
        await Task.Run(() => { using var wb = Decode(toon, options); wb.SaveAs(excelFilePath); }, cancellationToken).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts a worksheet's used range to a list of row dictionaries.
    /// The first used row provides the column header names.
    /// </summary>
    internal static List<Dictionary<string, object?>> WorksheetToRowList(IXLWorksheet worksheet)
    {
        var result = new List<Dictionary<string, object?>>();
        var usedRange = worksheet.RangeUsed();
        if (usedRange == null)
            return result;

        int firstRow = usedRange.FirstRow().RowNumber();
        int lastRow  = usedRange.LastRow().RowNumber();
        int firstCol = usedRange.FirstColumn().ColumnNumber();
        int lastCol  = usedRange.LastColumn().ColumnNumber();

        // Require at least one header row and one data row
        if (lastRow <= firstRow)
            return result;

        // Build header names from the first used row
        int colCount = lastCol - firstCol + 1;
        var headers  = new string[colCount];
        for (int col = firstCol; col <= lastCol; col++)
        {
            var name = worksheet.Cell(firstRow, col).GetString().Trim();
            headers[col - firstCol] = string.IsNullOrWhiteSpace(name)
                ? $"Column{col - firstCol + 1}"
                : name;
        }

        // Build data rows
        for (int row = firstRow + 1; row <= lastRow; row++)
        {
            var rowDict = new Dictionary<string, object?>(colCount);
            for (int col = firstCol; col <= lastCol; col++)
                rowDict[headers[col - firstCol]] = GetCellValue(worksheet.Cell(row, col));
            result.Add(rowDict);
        }

        return result;
    }

    /// <summary>Reads a typed value from a cell, returning null for blank cells.</summary>
    private static object? GetCellValue(IXLCell cell)
    {
        if (cell.IsEmpty())
            return null;

        return cell.DataType switch
        {
            XLDataType.Boolean  => cell.GetBoolean(),
            XLDataType.Number   => cell.GetDouble(),
            XLDataType.DateTime => cell.GetDateTime().ToString("O"),
            XLDataType.TimeSpan => cell.GetTimeSpan().ToString(),
            XLDataType.Text     => cell.GetString(),
            _                   => cell.GetString()
        };
    }

    /// <summary>Builds an <see cref="XLWorkbook"/> from a decoded TOON <see cref="JsonElement"/>.</summary>
    private static XLWorkbook JsonElementToWorkbook(JsonElement element)
    {
        var workbook = new XLWorkbook();

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var sheet = AddSheetWithUniqueName(workbook, property.Name);
                if (property.Value.ValueKind == JsonValueKind.Array)
                    PopulateWorksheet(sheet, property.Value);
                else
                    sheet.Cell(1, 1).Value = property.Value.GetRawText();
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            PopulateWorksheet(workbook.Worksheets.Add("Sheet1"), element);
        }

        if (!workbook.Worksheets.Any())
            workbook.Worksheets.Add("Sheet1");

        return workbook;
    }

    /// <summary>
    /// Writes an array <see cref="JsonElement"/> to a worksheet.
    /// The first object row's property names become the header row.
    /// For arrays of primitives a single <c>Value</c> column is used.
    /// </summary>
    private static void PopulateWorksheet(IXLWorksheet sheet, JsonElement arrayElement)
    {
        var rows = arrayElement.EnumerateArray().ToList();
        if (rows.Count == 0)
            return;

        // Find the first object row to derive column names
        var firstObjectRow = rows.FirstOrDefault(r => r.ValueKind == JsonValueKind.Object);

        if (firstObjectRow.ValueKind != JsonValueKind.Object)
        {
            // Array of primitives — write as a single "Value" column
            sheet.Cell(1, 1).Value = "Value";
            for (int r = 0; r < rows.Count; r++)
                SetCellValue(sheet.Cell(r + 2, 1), rows[r]);
            return;
        }

        var columns = firstObjectRow.EnumerateObject().Select(p => p.Name).ToList();

        // Header row
        for (int col = 0; col < columns.Count; col++)
            sheet.Cell(1, col + 1).Value = columns[col];

        // Data rows
        for (int r = 0; r < rows.Count; r++)
        {
            if (rows[r].ValueKind != JsonValueKind.Object)
                continue;

            for (int col = 0; col < columns.Count; col++)
            {
                if (rows[r].TryGetProperty(columns[col], out var val))
                    SetCellValue(sheet.Cell(r + 2, col + 1), val);
            }
        }
    }

    /// <summary>Writes a <see cref="JsonElement"/> value to a cell with appropriate typing.</summary>
    private static void SetCellValue(IXLCell cell, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Number:
                cell.Value = value.GetDouble();
                break;
            case JsonValueKind.String:
                var str = value.GetString() ?? string.Empty;
                // Restore DateTime values that were serialized as ISO 8601
                cell.Value = DateTime.TryParse(str, null, DateTimeStyles.RoundtripKind, out var dt)
                    ? (XLCellValue)dt
                    : str;
                break;
            case JsonValueKind.True:
                cell.Value = true;
                break;
            case JsonValueKind.False:
                cell.Value = false;
                break;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                cell.Value = Blank.Value;
                break;
            default:
                // Nested objects/arrays: store raw JSON text
                cell.Value = value.GetRawText();
                break;
        }
    }

    /// <summary>
    /// Adds a worksheet with a sanitized, unique name. If the sanitized name already exists,
    /// a numeric suffix is appended (e.g. "Sales (2)").
    /// </summary>
    private static IXLWorksheet AddSheetWithUniqueName(XLWorkbook workbook, string name)
    {
        var baseName = SanitizeSheetName(name);
        if (!workbook.Worksheets.Any(ws => ws.Name == baseName))
            return workbook.Worksheets.Add(baseName);

        for (int n = 2; n <= 999; n++)
        {
            var suffix    = $" ({n})";
            var maxBase   = 31 - suffix.Length;
            var candidate = (baseName.Length > maxBase ? baseName[..maxBase] : baseName) + suffix;
            if (!workbook.Worksheets.Any(ws => ws.Name == candidate))
                return workbook.Worksheets.Add(candidate);
        }

        // Fallback: let ClosedXML throw for the truly unusual case
        return workbook.Worksheets.Add(baseName);
    }

    /// <summary>
    /// Strips characters that Excel forbids in sheet names and truncates to 31 characters.
    /// Forbidden characters: <c>\ / ? * [ ] :</c>
    /// </summary>
    private static string SanitizeSheetName(string name)
    {
        var sanitized = string.Concat(
            name.Where(c => c != '\\' && c != '/' && c != '?' && c != '*'
                         && c != '[' && c != ']' && c != ':')
                .Take(31));
        return string.IsNullOrWhiteSpace(sanitized) ? "Sheet" : sanitized.Trim();
    }
}