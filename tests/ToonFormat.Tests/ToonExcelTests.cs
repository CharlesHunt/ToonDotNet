using System.Text.Json;

using ClosedXML.Excel;

namespace ToonFormat.Excel.Tests;

/// <summary>
/// Tests for ToonExcel static methods and ExcelToonExtensions.
/// Covers encoding (worksheet/workbook → TOON), decoding (TOON → workbook),
/// file operations, extension methods, and round-trips.
/// </summary>
public class ToonExcelTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    private string GetTempPath(string extension = ".xlsx")
    {
        var path = Path.Combine(Path.GetTempPath(), $"toon_excel_{Guid.NewGuid()}{extension}");
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
                try { File.Delete(file); } catch { }
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Single-sheet workbook: Products with id, name, price.</summary>
    private static XLWorkbook CreateProductsWorkbook()
    {
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Products");
        ws.Cell(1, 1).Value = "id";
        ws.Cell(1, 2).Value = "name";
        ws.Cell(1, 3).Value = "price";
        ws.Cell(2, 1).Value = 1;
        ws.Cell(2, 2).Value = "Widget";
        ws.Cell(2, 3).Value = 9.99;
        ws.Cell(3, 1).Value = 2;
        ws.Cell(3, 2).Value = "Gadget";
        ws.Cell(3, 3).Value = 19.99;
        return wb;
    }

    /// <summary>Two-sheet workbook: Products and Customers.</summary>
    private static XLWorkbook CreateMultiSheetWorkbook()
    {
        var wb = new XLWorkbook();

        var products = wb.Worksheets.Add("Products");
        products.Cell(1, 1).Value = "id";
        products.Cell(1, 2).Value = "name";
        products.Cell(2, 1).Value = 1;
        products.Cell(2, 2).Value = "Widget";

        var customers = wb.Worksheets.Add("Customers");
        customers.Cell(1, 1).Value = "id";
        customers.Cell(1, 2).Value = "name";
        customers.Cell(2, 1).Value = 1;
        customers.Cell(2, 2).Value = "Alice";

        return wb;
    }

    // =========================================================================
    // ToonExcel.Encode(IXLWorksheet)
    // =========================================================================

    [Fact]
    public void Encode_NullWorksheet_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ToonExcel.Encode((IXLWorksheet)null!));
    }

    [Fact]
    public void Encode_EmptyWorksheet_DoesNotThrow()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Empty");

        var result = ToonExcel.Encode(ws);

        Assert.NotNull(result);
    }

    [Fact]
    public void Encode_WorksheetWithOnlyHeaderRow_ProducesEmptyDataResult()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "id";
        ws.Cell(1, 2).Value = "name";

        var result = ToonExcel.Encode(ws);

        // WorksheetToRowList returns empty list when there are no data rows
        Assert.NotNull(result);
        var decoded = Toon.Decode(result);
        Assert.Equal(0, decoded.GetArrayLength());
    }

    [Fact]
    public void Encode_Worksheet_ProducesCorrectRowCount()
    {
        using var wb = CreateProductsWorkbook();

        var result = ToonExcel.Encode(wb.Worksheet("Products"));

        var decoded = Toon.Decode(result);
        Assert.Equal(2, decoded.GetArrayLength());
    }

    [Fact]
    public void Encode_Worksheet_PreservesColumnHeaders()
    {
        using var wb = CreateProductsWorkbook();

        var result = ToonExcel.Encode(wb.Worksheet("Products"));

        var first = Toon.Decode(result)[0];
        Assert.True(first.TryGetProperty("id", out _));
        Assert.True(first.TryGetProperty("name", out _));
        Assert.True(first.TryGetProperty("price", out _));
    }

    [Fact]
    public void Encode_Worksheet_PreservesStringValues()
    {
        using var wb = CreateProductsWorkbook();

        var result = ToonExcel.Encode(wb.Worksheet("Products"));

        var rows = Toon.Decode(result).EnumerateArray().ToArray();
        Assert.Equal("Widget", rows[0].GetProperty("name").GetString());
        Assert.Equal("Gadget", rows[1].GetProperty("name").GetString());
    }

    [Fact]
    public void Encode_Worksheet_PreservesNumericValues()
    {
        using var wb = CreateProductsWorkbook();

        var result = ToonExcel.Encode(wb.Worksheet("Products"));

        var rows = Toon.Decode(result).EnumerateArray().ToArray();
        Assert.Equal(9.99, rows[0].GetProperty("price").GetDouble(), 2);
        Assert.Equal(19.99, rows[1].GetProperty("price").GetDouble(), 2);
    }

    [Fact]
    public void Encode_WorksheetWithBooleans_PreservesBooleanValues()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "name";
        ws.Cell(1, 2).Value = "active";
        ws.Cell(2, 1).Value = "Alice";
        ws.Cell(2, 2).Value = true;
        ws.Cell(3, 1).Value = "Bob";
        ws.Cell(3, 2).Value = false;

        var result = ToonExcel.Encode(ws);

        var rows = Toon.Decode(result).EnumerateArray().ToArray();
        Assert.True(rows[0].GetProperty("active").GetBoolean());
        Assert.False(rows[1].GetProperty("active").GetBoolean());
    }

    [Fact]
    public void Encode_WorksheetWithBlankCell_ProducesNullValue()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "id";
        ws.Cell(1, 2).Value = "notes";
        ws.Cell(2, 1).Value = 1;
        // notes cell intentionally left blank

        var result = ToonExcel.Encode(ws);

        var first = Toon.Decode(result)[0];
        Assert.Equal(JsonValueKind.Null, first.GetProperty("notes").ValueKind);
    }

    [Fact]
    public void Encode_WorksheetWithBlankColumnHeader_FallsBackToColumnN()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "id";
        ws.Cell(1, 2).Value = "";   // blank — should become "Column2"
        ws.Cell(2, 1).Value = 1;
        ws.Cell(2, 2).Value = "value";

        var result = ToonExcel.Encode(ws);

        var first = Toon.Decode(result)[0];
        Assert.True(first.TryGetProperty("Column2", out _));
    }

    [Fact]
    public void Encode_WorksheetWithDateTime_SerializesAsIso8601String()
    {
        var dt = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "created";
        ws.Cell(2, 1).Value = dt;

        var result = ToonExcel.Encode(ws);

        var first = Toon.Decode(result)[0];
        Assert.Equal(JsonValueKind.String, first.GetProperty("created").ValueKind);
    }

    // =========================================================================
    // ToonExcel.Encode(IXLWorkbook)
    // =========================================================================

    [Fact]
    public void Encode_NullWorkbook_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ToonExcel.Encode((IXLWorkbook)null!));
    }

    [Fact]
    public void Encode_SingleSheetWorkbook_ProducesRootObjectWithSheetKey()
    {
        using var wb = CreateProductsWorkbook();

        var result = ToonExcel.Encode(wb);

        var decoded = Toon.Decode(result);
        Assert.Equal(JsonValueKind.Object, decoded.ValueKind);
        Assert.True(decoded.TryGetProperty("Products", out _));
    }

    [Fact]
    public void Encode_MultipleSheetWorkbook_ProducesOneKeyPerSheet()
    {
        using var wb = CreateMultiSheetWorkbook();

        var result = ToonExcel.Encode(wb);

        var decoded = Toon.Decode(result);
        Assert.True(decoded.TryGetProperty("Products", out _));
        Assert.True(decoded.TryGetProperty("Customers", out _));
    }

    [Fact]
    public void Encode_MultipleSheetWorkbook_PreservesDataPerSheet()
    {
        using var wb = CreateMultiSheetWorkbook();

        var result = ToonExcel.Encode(wb);

        var decoded = Toon.Decode(result);
        Assert.Equal("Widget", decoded.GetProperty("Products")[0].GetProperty("name").GetString());
        Assert.Equal("Alice",  decoded.GetProperty("Customers")[0].GetProperty("name").GetString());
    }

    [Fact]
    public void Encode_Workbook_WithCustomDelimiter_UsesDelimiter()
    {
        using var wb = CreateProductsWorkbook();
        var opts = new EncodeOptions { Delimiter = '|' };

        var result = ToonExcel.Encode(wb, opts);

        Assert.Contains("|", result);
    }

    // =========================================================================
    // ToonExcel.Decode(string)
    // =========================================================================

    [Fact]
    public void Decode_NullString_ThrowsNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ToonExcel.Decode(null!));
    }

    [Fact]
    public void Decode_EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ToonExcel.Decode(""));
    }

    [Fact]
    public void Decode_RootArrayToon_CreatesSingleWorksheet()
    {
        using var wb = ToonExcel.Decode("[2]{id,name}:\n  1,Alice\n  2,Bob");

        Assert.Single(wb.Worksheets);
    }

    [Fact]
    public void Decode_RootArrayToon_SheetIsNamedSheet1()
    {
        using var wb = ToonExcel.Decode("[2]{id,name}:\n  1,Alice\n  2,Bob");

        Assert.Equal("Sheet1", wb.Worksheet(1).Name);
    }

    [Fact]
    public void Decode_RootArrayToon_WritesHeaderRow()
    {
        using var wb = ToonExcel.Decode("[2]{id,name}:\n  1,Alice\n  2,Bob");

        var ws = wb.Worksheet(1);
        Assert.Equal("id",   ws.Cell(1, 1).GetString());
        Assert.Equal("name", ws.Cell(1, 2).GetString());
    }

    [Fact]
    public void Decode_RootArrayToon_WritesDataRows()
    {
        using var wb = ToonExcel.Decode("[2]{id,name}:\n  1,Alice\n  2,Bob");

        var ws = wb.Worksheet(1);
        Assert.Equal("Alice", ws.Cell(2, 2).GetString());
        Assert.Equal("Bob",   ws.Cell(3, 2).GetString());
    }

    [Fact]
    public void Decode_RootObjectToon_CreatesOneWorksheetPerKey()
    {
        var toon = "Products[1]{id,name}:\n  1,Widget\nCustomers[1]{id,name}:\n  1,Alice";

        using var wb = ToonExcel.Decode(toon);

        Assert.Equal(2, wb.Worksheets.Count);
        Assert.NotNull(wb.Worksheet("Products"));
        Assert.NotNull(wb.Worksheet("Customers"));
    }

    [Fact]
    public void Decode_NumberValues_WritesNumericCells()
    {
        using var wb = ToonExcel.Decode("[1]{id,amount}:\n  42,9.99");

        var ws = wb.Worksheet(1);
        Assert.Equal(XLDataType.Number, ws.Cell(2, 1).DataType);
        Assert.Equal(42.0, ws.Cell(2, 1).GetDouble());
    }

    [Fact]
    public void Decode_BooleanValues_WritesBooleanCells()
    {
        using var wb = ToonExcel.Decode("[2]{name,active}:\n  Alice,true\n  Bob,false");

        var ws = wb.Worksheet(1);
        Assert.Equal(XLDataType.Boolean, ws.Cell(2, 2).DataType);
        Assert.True(ws.Cell(2, 2).GetBoolean());
        Assert.False(ws.Cell(3, 2).GetBoolean());
    }

    [Fact]
    public void Decode_NullValues_WritesEmptyCells()
    {
        using var wb = ToonExcel.Decode("[1]{id,notes}:\n  1,null");

        Assert.True(wb.Worksheet(1).Cell(2, 2).IsEmpty());
    }

    [Fact]
    public void Decode_PrimitiveArray_UsesValueColumnHeader()
    {
        using var wb = ToonExcel.Decode("[3]: 1,2,3");

        Assert.Equal("Value", wb.Worksheet(1).Cell(1, 1).GetString());
    }

    [Fact]
    public void Decode_PrimitiveArray_WritesValuesInValueColumn()
    {
        using var wb = ToonExcel.Decode("[3]: 1,2,3");

        var ws = wb.Worksheet(1);
        Assert.Equal(1.0, ws.Cell(2, 1).GetDouble());
        Assert.Equal(2.0, ws.Cell(3, 1).GetDouble());
        Assert.Equal(3.0, ws.Cell(4, 1).GetDouble());
    }

    // =========================================================================
    // ToonExcel.EncodeFile
    // =========================================================================

    [Fact]
    public void EncodeFile_EmptyPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ToonExcel.EncodeFile(""));
    }

    [Fact]
    public void EncodeFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => ToonExcel.EncodeFile("does_not_exist.xlsx"));
    }

    [Fact]
    public void EncodeFile_ValidFile_ReturnsToonString()
    {
        var path = GetTempPath(".xlsx");
        using (var wb = CreateProductsWorkbook())
            wb.SaveAs(path);

        var result = ToonExcel.EncodeFile(path);

        Assert.NotEmpty(result);
        Assert.True(Toon.IsValid(result));
    }

    [Fact]
    public void EncodeFile_ValidFile_ContainsSheetKey()
    {
        var path = GetTempPath(".xlsx");
        using (var wb = CreateProductsWorkbook())
            wb.SaveAs(path);

        var result = ToonExcel.EncodeFile(path);

        var decoded = Toon.Decode(result);
        Assert.True(decoded.TryGetProperty("Products", out _));
    }

    // =========================================================================
    // ToonExcel.SaveAsToon
    // =========================================================================

    [Fact]
    public void SaveAsToon_EmptyToonPath_ThrowsArgumentException()
    {
        var xlsxPath = GetTempPath(".xlsx");
        using (var wb = CreateProductsWorkbook())
            wb.SaveAs(xlsxPath);

        Assert.Throws<ArgumentException>(() => ToonExcel.SaveAsToon(xlsxPath, ""));
    }

    [Fact]
    public void SaveAsToon_NonExistentExcelFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            ToonExcel.SaveAsToon("missing.xlsx", GetTempPath(".toon")));
    }

    [Fact]
    public void SaveAsToon_ValidInputs_CreatesToonFile()
    {
        var xlsxPath = GetTempPath(".xlsx");
        var toonPath = GetTempPath(".toon");
        using (var wb = CreateProductsWorkbook())
            wb.SaveAs(xlsxPath);

        ToonExcel.SaveAsToon(xlsxPath, toonPath);

        Assert.True(File.Exists(toonPath));
        Assert.NotEmpty(File.ReadAllText(toonPath));
    }

    // =========================================================================
    // ToonExcel.LoadToonFile
    // =========================================================================

    [Fact]
    public void LoadToonFile_EmptyPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ToonExcel.LoadToonFile(""));
    }

    [Fact]
    public void LoadToonFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => ToonExcel.LoadToonFile("missing.toon"));
    }

    [Fact]
    public void LoadToonFile_ValidFile_ReturnsWorkbookWithExpectedSheet()
    {
        var toonPath = GetTempPath(".toon");
        File.WriteAllText(toonPath, "[2]{id,name}:\n  1,Alice\n  2,Bob");

        using var wb = ToonExcel.LoadToonFile(toonPath);

        Assert.Single(wb.Worksheets);
        Assert.Equal("Sheet1", wb.Worksheet(1).Name);
    }

    [Fact]
    public void LoadToonFile_ValidFile_PreservesRowData()
    {
        var toonPath = GetTempPath(".toon");
        File.WriteAllText(toonPath, "[2]{id,name}:\n  1,Alice\n  2,Bob");

        using var wb = ToonExcel.LoadToonFile(toonPath);

        Assert.Equal("Alice", wb.Worksheet(1).Cell(2, 2).GetString());
    }

    // =========================================================================
    // ToonExcel.SaveAsExcel
    // =========================================================================

    [Fact]
    public void SaveAsExcel_EmptyPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ToonExcel.SaveAsExcel("[1]{id}:\n  1", ""));
    }

    [Fact]
    public void SaveAsExcel_ValidInputs_CreatesExcelFile()
    {
        var xlsxPath = GetTempPath(".xlsx");

        ToonExcel.SaveAsExcel("[2]{id,name}:\n  1,Alice\n  2,Bob", xlsxPath);

        Assert.True(File.Exists(xlsxPath));
    }

    [Fact]
    public void SaveAsExcel_ValidInputs_FileIsReadableWorkbook()
    {
        var xlsxPath = GetTempPath(".xlsx");
        ToonExcel.SaveAsExcel("[2]{id,name}:\n  1,Alice\n  2,Bob", xlsxPath);

        using var wb = new XLWorkbook(xlsxPath);

        Assert.Equal("Alice", wb.Worksheet(1).Cell(2, 2).GetString());
    }

    // =========================================================================
    // ToonExcel.ConvertToonToExcel
    // =========================================================================

    [Fact]
    public void ConvertToonToExcel_NonExistentToonFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            ToonExcel.ConvertToonToExcel("missing.toon", GetTempPath(".xlsx")));
    }

    [Fact]
    public void ConvertToonToExcel_EmptyExcelPath_ThrowsArgumentException()
    {
        var toonPath = GetTempPath(".toon");
        File.WriteAllText(toonPath, "[1]{id}:\n  1");

        Assert.Throws<ArgumentException>(() => ToonExcel.ConvertToonToExcel(toonPath, ""));
    }

    [Fact]
    public void ConvertToonToExcel_ValidFiles_CreatesExcelFile()
    {
        var toonPath = GetTempPath(".toon");
        var xlsxPath = GetTempPath(".xlsx");
        File.WriteAllText(toonPath, "[1]{id,name}:\n  1,Widget");

        ToonExcel.ConvertToonToExcel(toonPath, xlsxPath);

        Assert.True(File.Exists(xlsxPath));
    }

    // =========================================================================
    // Extension methods
    // =========================================================================

    [Fact]
    public void ToToon_Worksheet_ProducesSameResultAsStaticEncode()
    {
        using var wb = CreateProductsWorkbook();

        var viaExtension = wb.Worksheet("Products").ToToon();
        var viaStatic    = ToonExcel.Encode(wb.Worksheet("Products"));

        Assert.Equal(viaStatic, viaExtension);
    }

    [Fact]
    public void ToToon_Workbook_ProducesSameResultAsStaticEncode()
    {
        using var wb = CreateProductsWorkbook();

        var viaExtension = wb.ToToon();
        var viaStatic    = ToonExcel.Encode(wb);

        Assert.Equal(viaStatic, viaExtension);
    }

    [Fact]
    public void ToExcelWorkbook_ValidToon_ReturnsWorkbookWithCorrectSheet()
    {
        var toon = "[2]{id,name}:\n  1,Alice\n  2,Bob";

        using var wb = toon.ToExcelWorkbook();

        Assert.Equal("Sheet1", wb.Worksheet(1).Name);
    }

    [Fact]
    public void ToExcelWorkbook_WithOptions_AppliesOptions()
    {
        var toon = "[1]{id,name}:\n  1,Alice";

        using var wb = toon.ToExcelWorkbook(new DecodeOptions { Strict = false });

        Assert.NotNull(wb.Worksheet(1));
    }

    // =========================================================================
    // Round-trip
    // =========================================================================

    [Fact]
    public void RoundTrip_Worksheet_PreservesRowCount()
    {
        using var original = CreateProductsWorkbook();
        var ws = original.Worksheet("Products");

        var toon = ToonExcel.Encode(ws);
        using var decoded = ToonExcel.Decode(toon);

        // Header row + 2 data rows = row 3 is the last used
        Assert.Equal(3, decoded.Worksheet(1).LastRowUsed()!.RowNumber());
    }

    [Fact]
    public void RoundTrip_Worksheet_PreservesStringValues()
    {
        using var original = CreateProductsWorkbook();
        var ws = original.Worksheet("Products");

        var toon = ToonExcel.Encode(ws);
        using var decoded = ToonExcel.Decode(toon);

        Assert.Equal("Widget", decoded.Worksheet(1).Cell(2, 2).GetString());
        Assert.Equal("Gadget", decoded.Worksheet(1).Cell(3, 2).GetString());
    }

    [Fact]
    public void RoundTrip_Worksheet_PreservesNumericValues()
    {
        using var original = CreateProductsWorkbook();
        var ws = original.Worksheet("Products");

        var toon = ToonExcel.Encode(ws);
        using var decoded = ToonExcel.Decode(toon);

        Assert.Equal(9.99,  decoded.Worksheet(1).Cell(2, 3).GetDouble(), 2);
        Assert.Equal(19.99, decoded.Worksheet(1).Cell(3, 3).GetDouble(), 2);
    }

    [Fact]
    public void RoundTrip_Workbook_PreservesAllSheetNames()
    {
        using var original = CreateMultiSheetWorkbook();

        var toon = ToonExcel.Encode(original);
        using var decoded = ToonExcel.Decode(toon);

        Assert.NotNull(decoded.Worksheet("Products"));
        Assert.NotNull(decoded.Worksheet("Customers"));
    }

    [Fact]
    public void RoundTrip_Workbook_PreservesDataAcrossSheets()
    {
        using var original = CreateMultiSheetWorkbook();

        var toon = ToonExcel.Encode(original);
        using var decoded = ToonExcel.Decode(toon);

        Assert.Equal("Widget", decoded.Worksheet("Products").Cell(2, 2).GetString());
        Assert.Equal("Alice",  decoded.Worksheet("Customers").Cell(2, 2).GetString());
    }

    [Fact]
    public void RoundTrip_FileOperations_PreservesData()
    {
        var xlsxIn  = GetTempPath(".xlsx");
        var toonPath = GetTempPath(".toon");
        var xlsxOut  = GetTempPath(".xlsx");

        using (var wb = CreateProductsWorkbook())
            wb.SaveAs(xlsxIn);

        ToonExcel.SaveAsToon(xlsxIn, toonPath);
        ToonExcel.ConvertToonToExcel(toonPath, xlsxOut);

        using var result = new XLWorkbook(xlsxOut);
        Assert.Equal("Widget", result.Worksheet(1).Cell(2, 2).GetString());
    }

    [Fact]
    public void RoundTrip_WithBooleans_PreservesBooleanValues()
    {
        using var original = new XLWorkbook();
        var ws = original.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "name";
        ws.Cell(1, 2).Value = "active";
        ws.Cell(2, 1).Value = "Alice";
        ws.Cell(2, 2).Value = true;
        ws.Cell(3, 1).Value = "Bob";
        ws.Cell(3, 2).Value = false;

        var toon = ToonExcel.Encode(ws);
        using var decoded = ToonExcel.Decode(toon);

        Assert.True(decoded.Worksheet(1).Cell(2, 2).GetBoolean());
        Assert.False(decoded.Worksheet(1).Cell(3, 2).GetBoolean());
    }
}
