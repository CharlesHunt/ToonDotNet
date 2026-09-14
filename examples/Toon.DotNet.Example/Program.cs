using System.Text.Json;
using ClosedXML.Excel;

using ToonFormat;
using ToonFormat.Csv;
using ToonFormat.Excel;

Console.WriteLine("=== ToonFormat .NET Example ===\n");

// Example 1: Simple object encoding
var person = new { name = "Alice", age = 30, isActive = true };
string toonString = Toon.Encode(person);

Console.WriteLine("1. Simple Object:");
Console.WriteLine($"Original: {JsonSerializer.Serialize(person)}");
Console.WriteLine($"TOON:     {toonString}");
Console.WriteLine();

// Example 2: Tabular data (most efficient for uniform arrays)
var userData = new 
{
    users = new[]
    {
        new { id = 1, name = "Alice", role = "admin", active = true },
        new { id = 2, name = "Bob", role = "user", active = false },
        new { id = 3, name = "Charlie", role = "moderator", active = true }
    }
};

string tabularToon = Toon.Encode(userData);
Console.WriteLine("2. Tabular Data (Uniform Array):");
Console.WriteLine($"JSON ({JsonSerializer.Serialize(userData).Length} chars):");
Console.WriteLine(JsonSerializer.Serialize(userData, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"\nTOON ({tabularToon.Length} chars - {100 - (tabularToon.Length * 100 / JsonSerializer.Serialize(userData).Length)}% smaller):");
Console.WriteLine(tabularToon);
Console.WriteLine();

// Example 3: Decoding back to objects
Console.WriteLine("3. Round-trip Decoding:");
JsonElement decoded = Toon.Decode(tabularToon);
var users = decoded.GetProperty("users").EnumerateArray().ToArray();
Console.WriteLine($"Decoded {users.Length} users:");
foreach (var user in users)
{
    Console.WriteLine($"  - {user.GetProperty("name").GetString()}: {user.GetProperty("role").GetString()}");
}
Console.WriteLine();

// Example 4: Strongly-typed decoding
Console.WriteLine("4. Strongly-typed Decoding:");
var typedResult = Toon.Decode<UserData>(tabularToon);
Console.WriteLine($"Typed result has {typedResult.Users.Length} users:");
foreach (var user in typedResult.Users)
{
    Console.WriteLine($"  - {user.Name} (ID: {user.Id}, Role: {user.Role}, Active: {user.Active})");
}
Console.WriteLine();

// Example 5: Different delimiters and options
Console.WriteLine("5. Custom Options (Pipe delimiter, Length markers):");
var options = new EncodeOptions 
{ 
    Delimiter = '|', 
    LengthMarker = '#',
    Indent = 4 
};
string customToon = Toon.Encode(userData, options);
Console.WriteLine(customToon);
Console.WriteLine();

// Example 6: Size comparison percentage
Console.WriteLine("6. Size Comparison Percentage:");

decimal sizeReduction = Toon.SizeComparisonPercentage(userData, null);
Console.WriteLine($"User data example is {sizeReduction:#0.00}% of the equivalent JSON");

Console.WriteLine();

// ---------------------------------------------------------------------------
// Helper: build a reusable in-memory workbook with two sheets.
// ---------------------------------------------------------------------------
static XLWorkbook BuildSampleWorkbook()
{
    var wb = new XLWorkbook();

    var products = wb.Worksheets.Add("Products");
    products.Cell(1, 1).Value = "id";   products.Cell(1, 2).Value = "name";    products.Cell(1, 3).Value = "price";
    products.Cell(2, 1).Value = 1;      products.Cell(2, 2).Value = "Widget";  products.Cell(2, 3).Value = 9.99;
    products.Cell(3, 1).Value = 2;      products.Cell(3, 2).Value = "Gadget";  products.Cell(3, 3).Value = 19.99;
    products.Cell(4, 1).Value = 3;      products.Cell(4, 2).Value = "Doohickey"; products.Cell(4, 3).Value = 4.99;

    var customers = wb.Worksheets.Add("Customers");
    customers.Cell(1, 1).Value = "id";  customers.Cell(1, 2).Value = "name";   customers.Cell(1, 3).Value = "active";
    customers.Cell(2, 1).Value = 1;     customers.Cell(2, 2).Value = "Alice";  customers.Cell(2, 3).Value = true;
    customers.Cell(3, 1).Value = 2;     customers.Cell(3, 2).Value = "Bob";    customers.Cell(3, 3).Value = false;

    return wb;
}

// ---------------------------------------------------------------------------
// Excel Examples
// ---------------------------------------------------------------------------
Console.WriteLine("\n=== ToonDotNet.Excel Examples ===\n");

// Example 7: Encode a single worksheet to TOON
Console.WriteLine("7. Encode a Single Worksheet to TOON:");
using (var wb = BuildSampleWorkbook())
{
    string toon = ToonExcel.Encode(wb.Worksheet("Products"));
    Console.WriteLine("Products worksheet encoded to TOON:");
    Console.WriteLine(toon);
}
Console.WriteLine();

// Example 8: Encode a full workbook to TOON
Console.WriteLine("8. Encode a Full Workbook (multiple sheets) to TOON:");
using (var wb = BuildSampleWorkbook())
{
    string toon = ToonExcel.Encode(wb);
    Console.WriteLine("Workbook (Products + Customers) encoded to TOON:");
    Console.WriteLine(toon);
}
Console.WriteLine();

// Example 9: Decode TOON back to an Excel workbook
Console.WriteLine("9. Decode TOON Back to an Excel Workbook:");
using (var original = BuildSampleWorkbook())
{
    string toon = ToonExcel.Encode(original);

    using var decodedWb = ToonExcel.Decode(toon);
    Console.WriteLine($"Decoded workbook has {decodedWb.Worksheets.Count} sheet(s):");
    foreach (var ws in decodedWb.Worksheets)
    {
        int dataRows = (ws.LastRowUsed()?.RowNumber() ?? 1) - 1;
        Console.WriteLine($"  Sheet '{ws.Name}': {dataRows} data row(s)");
        Console.WriteLine($"    First row: {ws.Cell(2, 1).GetDouble()}, {ws.Cell(2, 2).GetString()}");
    }
}
Console.WriteLine();

// Example 10: Extension methods — ToToon() and ToExcelWorkbook()
Console.WriteLine("10. Extension Methods (ToToon / ToExcelWorkbook):");
using (var wb = BuildSampleWorkbook())
{
    // Worksheet extension
    string worksheetToon = wb.Worksheet("Customers").ToToon();
    Console.WriteLine("Customers worksheet via .ToToon():");
    Console.WriteLine(worksheetToon);

    // String extension
    using var roundTripped = worksheetToon.ToExcelWorkbook();
    Console.WriteLine($"Round-tripped via .ToExcelWorkbook() — sheet name: '{roundTripped.Worksheet(1).Name}'");
    Console.WriteLine($"  Alice active: {roundTripped.Worksheet(1).Cell(2, 3).GetBoolean()}");
    Console.WriteLine($"  Bob   active: {roundTripped.Worksheet(1).Cell(3, 3).GetBoolean()}");
}
Console.WriteLine();

// Example 11: File operations — SaveAsToon and ConvertToonToExcel
Console.WriteLine("11. File Operations (Excel ↔ TOON file round-trip):");
var xlsxPath  = Path.Combine(Path.GetTempPath(), "toon_example.xlsx");
var toonPath  = Path.Combine(Path.GetTempPath(), "toon_example.toon");
var xlsxPath2 = Path.Combine(Path.GetTempPath(), "toon_example_out.xlsx");
try
{
    using (var wb = BuildSampleWorkbook())
        wb.SaveAs(xlsxPath);
    ToonExcel.SaveAsToon(xlsxPath, toonPath);
    Console.WriteLine($"Saved '{Path.GetFileName(xlsxPath)}' → '{Path.GetFileName(toonPath)}'");
    Console.WriteLine($"TOON file size: {new FileInfo(toonPath).Length} bytes");
    Console.WriteLine($"xlsx file size: {new FileInfo(xlsxPath).Length} bytes");

    ToonExcel.ConvertToonToExcel(toonPath, xlsxPath2);
    Console.WriteLine($"Converted '{Path.GetFileName(toonPath)}' → '{Path.GetFileName(xlsxPath2)}'");

    using var result = new XLWorkbook(xlsxPath2);
    Console.WriteLine($"Verified: '{result.Worksheet("Products").Cell(2, 2).GetString()}' in restored workbook");
}
finally
{
    foreach (var f in new[] { xlsxPath, toonPath, xlsxPath2 })
        if (File.Exists(f)) File.Delete(f);
}
Console.WriteLine();

// Example 12: Custom encode options with a pipe delimiter
Console.WriteLine("12. Custom Options (pipe delimiter, length marker):");
using (var wb = BuildSampleWorkbook())
{
    var opts = new EncodeOptions { Delimiter = '|', LengthMarker = '#' };
    string toon = wb.Worksheet("Products").ToToon(opts);
    Console.WriteLine("Products worksheet with '|' delimiter and '#' length marker:");
    Console.WriteLine(toon);
}
Console.WriteLine();

// ---------------------------------------------------------------------------
// TOON V4 Feature Examples
// ---------------------------------------------------------------------------
Console.WriteLine("\n=== TOON V4 Feature Examples ===\n");

// Example 13: Nested field groups (v4.0.0 RFC #46)
Console.WriteLine("13. Nested Field Groups (v4.0.0 RFC #46):");
var orders = new
{
    orders = new[]
    {
        new { id = 1, customer = new { name = "Ada", country = "DK" }, total = 99.00 },
        new { id = 2, customer = new { name = "Bob", country = "UK" }, total = 149.00 }
    }
};
string nestedFieldGroupToon = Toon.Encode(orders);
Console.WriteLine("A nested-uniform column (\"customer\") collapses into a nested field group header:");
Console.WriteLine(nestedFieldGroupToon);
Console.WriteLine();

// Example 14: Keyed tabular form for objects (v4.0.0 RFC #57)
Console.WriteLine("14. Keyed Tabular Form for Objects (v4.0.0 RFC #57):");
var usersByUsername = new
{
    users = new Dictionary<string, object>
    {
        ["alice"] = new { age = 30, city = "Berlin" },
        ["bob"] = new { age = 25, city = "Oslo" }
    }
};
string keyedTabularToon = Toon.Encode(usersByUsername);
Console.WriteLine("An object of uniform objects collapses into keyed tabular form (note the \"[N:]\" header):");
Console.WriteLine(keyedTabularToon);
Console.WriteLine();

// Example 15: EncodeOptions.SpecVersion — V4 (default) vs V3 (legacy) output
Console.WriteLine("15. EncodeOptions.SpecVersion — V4 (default) vs V3 (legacy compatibility):");
Console.WriteLine("V4 (default) output uses the nested field group from Example 13:");
Console.WriteLine(Toon.Encode(orders));
Console.WriteLine("V3 output (byte-for-byte v3.3.2 compatible) falls back to plain nested objects:");
Console.WriteLine(Toon.Encode(orders, new EncodeOptions { SpecVersion = ToonSpecVersion.V3 }));
Console.WriteLine();

// Example 16: Full-line comments (v4.0.0 §5.1) — decode-side, always understood
Console.WriteLine("16. Full-Line Comments (v4.0.0 §5.1):");
string toonWithComments =
    "# Employee roster\n" +
    "employees[2]{id,name}:\n" +
    "  1,Alice\n" +
    "  # Bob just joined\n" +
    "  2,Bob";
var decodedWithComments = Toon.Decode(toonWithComments);
Console.WriteLine("Source TOON (with comments):");
Console.WriteLine(toonWithComments);
Console.WriteLine($"\nDecoded (comments stripped): {JsonSerializer.Serialize(decodedWithComments)}");
Console.WriteLine();

// Example 17: DecodeOptions.LegacyCompatibility — token trimming scope
Console.WriteLine("17. DecodeOptions.LegacyCompatibility — token trimming scope:");
string toonWithTab = "name: \tAlice"; // a tab immediately after the colon
var trimmedDefault = Toon.Decode(toonWithTab);
var trimmedLegacy = Toon.Decode(toonWithTab, new DecodeOptions { LegacyCompatibility = true });
Console.WriteLine("Default (v4) decoding trims only U+0020, so the tab is preserved in the value:");
Console.WriteLine($"  \"{trimmedDefault.GetProperty("name").GetString()}\"");
Console.WriteLine("LegacyCompatibility = true restores the old broader-whitespace trim:");
Console.WriteLine($"  \"{trimmedLegacy.GetProperty("name").GetString()}\"");
Console.WriteLine();

// Example 18: Excel decoding of keyed tabular form to a proper worksheet
Console.WriteLine("18. Excel: Decoding Keyed Tabular Form to a Proper Worksheet:");
using (var wb = ToonExcel.Decode(keyedTabularToon))
{
    var sheet = wb.Worksheet("users");
    Console.WriteLine($"Header row: {sheet.Cell(1, 1).GetString()}, {sheet.Cell(1, 2).GetString()}, {sheet.Cell(1, 3).GetString()}");
    Console.WriteLine($"  Row 1: {sheet.Cell(2, 1).GetString()}, age={sheet.Cell(2, 2).GetDouble()}, city={sheet.Cell(2, 3).GetString()}");
    Console.WriteLine($"  Row 2: {sheet.Cell(3, 1).GetString()}, age={sheet.Cell(3, 2).GetDouble()}, city={sheet.Cell(3, 3).GetString()}");
}
Console.WriteLine();

// ---------------------------------------------------------------------------
// Optional: save example CSV and Excel files to disk for manual inspection.
// The test suite's own CSV/Excel temp files are deleted immediately after
// each test runs, so this is the way to get real, persisted output files.
// ---------------------------------------------------------------------------
Console.WriteLine("=== Save Example Files ===\n");
Console.Write("Write example CSV and Excel files to the application's base directory? (y/n): ");
string? saveChoice = Console.ReadLine();

if (string.Equals(saveChoice?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
{
    string baseDir = AppContext.BaseDirectory;

    // CSV: the flat "users" tabular array from Example 2.
    string usersArrayToon = Toon.Encode(userData.users);
    string csvOutputPath = Path.Combine(baseDir, "example_users.csv");
    ToonCsv.ToCsvFile(usersArrayToon, csvOutputPath);
    Console.WriteLine($"CSV written to:                        {csvOutputPath}");

    // Excel: the sample multi-sheet workbook (Products + Customers).
    string workbookOutputPath = Path.Combine(baseDir, "example_workbook.xlsx");
    using (var wb = BuildSampleWorkbook())
        wb.SaveAs(workbookOutputPath);
    Console.WriteLine($"Excel workbook written to:             {workbookOutputPath}");

    // Excel: the Example 14/18 keyed-tabular-form data, decoded straight
    // to a worksheet — open this one to see the "key" column layout.
    string keyedTabularOutputPath = Path.Combine(baseDir, "example_keyed_tabular.xlsx");
    using (var wb = ToonExcel.Decode(keyedTabularToon))
        wb.SaveAs(keyedTabularOutputPath);
    Console.WriteLine($"Keyed-tabular-form Excel workbook written to: {keyedTabularOutputPath}");

    Console.WriteLine("\nOpen these files directly to inspect the output.");
}
else
{
    Console.WriteLine("Skipped — no files written.");
}
Console.WriteLine();

Console.ReadKey();

