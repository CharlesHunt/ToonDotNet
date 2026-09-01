using ClosedXML.Excel;

namespace ToonFormat.Excel;

/// <summary>
/// Extension methods that add TOON encoding and decoding capabilities directly to
/// ClosedXML worksheet, workbook, and string objects.
/// </summary>
public static class ExcelToonExtensions
{
    /// <summary>
    /// Encodes this worksheet to a TOON format string.
    /// The first row is used as column headers; subsequent rows become data rows.
    /// The output is a root TOON array.
    /// </summary>
    /// <param name="worksheet">The worksheet to encode.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <returns>A TOON format string representing the worksheet data.</returns>
    /// <example>
    /// <code>
    /// using var workbook = new XLWorkbook("report.xlsx");
    /// string toon = workbook.Worksheet("Sales").ToToon();
    /// </code>
    /// </example>
    public static string ToToon(this IXLWorksheet worksheet, EncodeOptions? options = null)
        => ToonExcel.Encode(worksheet, options);

    /// <summary>
    /// Encodes all worksheets in this workbook to TOON format.
    /// Each sheet becomes a named property in the resulting TOON object, keyed by sheet name.
    /// </summary>
    /// <param name="workbook">The workbook to encode.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <returns>A TOON format string where each top-level key is a sheet name.</returns>
    /// <example>
    /// <code>
    /// using var workbook = new XLWorkbook("report.xlsx");
    /// string toon = workbook.ToToon();
    /// File.WriteAllText("report.toon", toon);
    /// </code>
    /// </example>
    public static string ToToon(this IXLWorkbook workbook, EncodeOptions? options = null)
        => ToonExcel.Encode(workbook, options);

    /// <summary>
    /// Decodes this TOON string into a new <see cref="XLWorkbook"/>.
    /// <list type="bullet">
    ///   <item>Root TOON object → one worksheet per key, named after the key.</item>
    ///   <item>Root TOON array  → single worksheet named <c>Sheet1</c>.</item>
    /// </list>
    /// The caller is responsible for disposing the returned workbook.
    /// </summary>
    /// <param name="toonString">The TOON format string to decode.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <returns>A new <see cref="XLWorkbook"/> populated from the TOON data.</returns>
    /// <example>
    /// <code>
    /// string toon = File.ReadAllText("report.toon");
    /// using var workbook = toon.ToExcelWorkbook();
    /// workbook.SaveAs("report.xlsx");
    /// </code>
    /// </example>
    public static XLWorkbook ToExcelWorkbook(this string toonString, DecodeOptions? options = null)
        => ToonExcel.Decode(toonString, options);

    /// <summary>
    /// Asynchronously encodes this worksheet to a TOON format string.
    /// </summary>
    /// <param name="worksheet">The worksheet to encode.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <param name="cancellationToken">Token to cancel the operation before it starts.</param>
    /// <returns>A task whose result is the TOON format string.</returns>
    /// <example>
    /// <code>
    /// using var workbook = new XLWorkbook("report.xlsx");
    /// string toon = await workbook.Worksheet("Sales").ToToonAsync();
    /// </code>
    /// </example>
    public static Task<string> ToToonAsync(this IXLWorksheet worksheet, EncodeOptions? options = null, CancellationToken cancellationToken = default)
        => ToonExcel.EncodeAsync(worksheet, options, cancellationToken);

    /// <summary>
    /// Asynchronously encodes all worksheets in this workbook to TOON format.
    /// Each sheet becomes a named property in the resulting TOON object.
    /// </summary>
    /// <param name="workbook">The workbook to encode.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <param name="cancellationToken">Token to cancel the operation before it starts.</param>
    /// <returns>A task whose result is the TOON format string.</returns>
    /// <example>
    /// <code>
    /// using var workbook = new XLWorkbook("report.xlsx");
    /// string toon = await workbook.ToToonAsync();
    /// await File.WriteAllTextAsync("report.toon", toon);
    /// </code>
    /// </example>
    public static Task<string> ToToonAsync(this IXLWorkbook workbook, EncodeOptions? options = null, CancellationToken cancellationToken = default)
        => ToonExcel.EncodeAsync(workbook, options, cancellationToken);

    /// <summary>
    /// Asynchronously decodes this TOON string into a new <see cref="XLWorkbook"/>.
    /// <list type="bullet">
    ///   <item>Root TOON object → one worksheet per key, named after the key.</item>
    ///   <item>Root TOON array  → single worksheet named <c>Sheet1</c>.</item>
    /// </list>
    /// The caller is responsible for disposing the returned workbook.
    /// </summary>
    /// <param name="toonString">The TOON format string to decode.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <param name="cancellationToken">Token to cancel the operation before it starts.</param>
    /// <returns>A task whose result is a new <see cref="XLWorkbook"/> populated from the TOON data.</returns>
    /// <example>
    /// <code>
    /// string toon = await File.ReadAllTextAsync("report.toon");
    /// using var workbook = await toon.ToExcelWorkbookAsync();
    /// workbook.SaveAs("report.xlsx");
    /// </code>
    /// </example>
    public static Task<XLWorkbook> ToExcelWorkbookAsync(this string toonString, DecodeOptions? options = null, CancellationToken cancellationToken = default)
        => ToonExcel.DecodeAsync(toonString, options, cancellationToken);
}