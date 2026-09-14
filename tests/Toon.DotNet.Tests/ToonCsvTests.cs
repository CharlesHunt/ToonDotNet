using System.Text;
using System.Text.Json;

namespace ToonFormat.Csv.Tests;

/// <summary>
/// Tests for ToonCsv static methods and CsvToonExtensions.
/// Covers encoding (CSV → TOON), decoding (TOON → CSV), stream operations,
/// file operations, async methods, type coercion, round-trips, and extension methods.
/// </summary>
public class ToonCsvTests : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private readonly List<string> _tempDirectories = [];

    private string GetTempPath(string extension = ".csv")
    {
        var path = Path.Combine(Path.GetTempPath(), $"toon_csv_{Guid.NewGuid()}{extension}");
        _tempFiles.Add(path);
        return path;
    }

    private string GetTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"toon_csv_dir_{Guid.NewGuid()}");
        _tempDirectories.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
                try { File.Delete(file); } catch { }
        }

        foreach (var dir in _tempDirectories)
        {
            if (Directory.Exists(dir))
                try { Directory.Delete(dir, recursive: true); } catch { }
        }
    }

    // =========================================================================
    // ToonCsv.FromCsv(string)
    // =========================================================================

    [Fact]
    public void FromCsv_NullCsv_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ToonCsv.FromCsv((string)null!));
    }

    [Fact]
    public void FromCsv_EmptyCsv_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ToonCsv.FromCsv((string)string.Empty));
    }

    [Fact]
    public void FromCsv_ValidCsv_ReturnsValidToon()
    {
        const string csv = "id,name,role\n1,Alice,admin\n2,Bob,user";

        var toon = ToonCsv.FromCsv(csv);

        Assert.True(Toon.IsValid(toon));
    }

    [Fact]
    public void FromCsv_MultipleRows_EncodesAllRows()
    {
        const string csv = "id,name\n1,Alice\n2,Bob\n3,Carol";

        var toon = ToonCsv.FromCsv(csv);
        var element = Toon.Decode(toon);

        Assert.Equal(JsonValueKind.Array, element.ValueKind);
        Assert.Equal(3, element.GetArrayLength());
    }

    [Fact]
    public void FromCsv_PreservesStringFieldValues()
    {
        const string csv = "id,name,role\n1,Alice,admin";

        var toon = ToonCsv.FromCsv(csv);
        var row = Toon.Decode(toon)[0];

        Assert.Equal("Alice", row.GetProperty("name").GetString());
        Assert.Equal("admin", row.GetProperty("role").GetString());
    }

    [Fact]
    public void FromCsv_IntegerValues_AreParsedAsNumbers()
    {
        const string csv = "id,score\n1,42";

        var toon = ToonCsv.FromCsv(csv);
        var row = Toon.Decode(toon)[0];

        Assert.Equal(JsonValueKind.Number, row.GetProperty("id").ValueKind);
        Assert.Equal(1, row.GetProperty("id").GetInt64());
        Assert.Equal(42, row.GetProperty("score").GetInt64());
    }

    [Fact]
    public void FromCsv_FloatingPointValues_AreParsedAsNumbers()
    {
        const string csv = "product,price\nWidget,9.99";

        var toon = ToonCsv.FromCsv(csv);
        var row = Toon.Decode(toon)[0];

        Assert.Equal(JsonValueKind.Number, row.GetProperty("price").ValueKind);
        Assert.Equal(9.99, row.GetProperty("price").GetDouble());
    }

    // Spec §4 (TOON_V3.md finding, CSV-package-specific): a leading zero
    // followed by another digit means the value is a string in TOON's
    // number grammar, not a number. CoerceValue previously coerced such
    // cells to long/double before the value ever reached the encoder,
    // silently dropping the leading zero (e.g. a zip code becoming a
    // smaller integer).
    [Fact]
    public void FromCsv_LeadingZeroNumericCell_PreservedAsString()
    {
        const string csv = "id,zip\n1,05678";

        var toon = ToonCsv.FromCsv(csv);
        var row = Toon.Decode(toon)[0];

        Assert.Equal(JsonValueKind.String, row.GetProperty("zip").ValueKind);
        Assert.Equal("05678", row.GetProperty("zip").GetString());
    }

    [Fact]
    public void FromCsv_LeadingZeroDecimalCell_StillParsedAsNumber()
    {
        // "0.5" is explicitly NOT a forbidden leading zero (spec §4) —
        // guards against the fix above over-correcting.
        const string csv = "id,fraction\n1,0.5";

        var toon = ToonCsv.FromCsv(csv);
        var row = Toon.Decode(toon)[0];

        Assert.Equal(JsonValueKind.Number, row.GetProperty("fraction").ValueKind);
        Assert.Equal(0.5, row.GetProperty("fraction").GetDouble());
    }

    [Fact]
    public void RoundTrip_CsvWithLeadingZeroCell_PreservesLeadingZero()
    {
        const string csv = "id,zip\n1,05678";

        var toon = ToonCsv.FromCsv(csv);
        var roundTrippedCsv = ToonCsv.ToCsv(toon);

        Assert.Contains("05678", roundTrippedCsv);
    }

    [Fact]
    public void FromCsv_BooleanValues_AreParsedAsBooleans()
    {
        const string csv = "name,active\nAlice,true\nBob,false";

        var toon = ToonCsv.FromCsv(csv);
        var element = Toon.Decode(toon);

        Assert.Equal(JsonValueKind.True,  element[0].GetProperty("active").ValueKind);
        Assert.Equal(JsonValueKind.False, element[1].GetProperty("active").ValueKind);
    }

    [Fact]
    public void FromCsv_EmptyFields_AreEncodedAsNull()
    {
        const string csv = "id,name,role\n1,,admin";

        var toon = ToonCsv.FromCsv(csv);
        var row = Toon.Decode(toon)[0];

        Assert.Equal(JsonValueKind.Null, row.GetProperty("name").ValueKind);
    }

    [Fact]
    public void FromCsv_HeaderOnly_ReturnsEmptyArray()
    {
        const string csv = "id,name,role";

        var toon = ToonCsv.FromCsv(csv);
        var element = Toon.Decode(toon);

        Assert.Equal(JsonValueKind.Array, element.ValueKind);
        Assert.Equal(0, element.GetArrayLength());
    }

    [Fact]
    public void FromCsv_WithPipeDelimiterOption_AppliesDelimiter()
    {
        const string csv = "id,name\n1,Alice";
        var opts = new EncodeOptions { Delimiter = '|' };

        var toon = ToonCsv.FromCsv(csv, opts);

        Assert.Contains("|", toon);
    }

    [Fact]
    public void FromCsv_QuotedFieldWithComma_ParsedAsSingleValue()
    {
        const string csv = "id,name\n1,\"Smith, Alice\"";

        var toon = ToonCsv.FromCsv(csv);
        var row = Toon.Decode(toon)[0];

        Assert.Equal("Smith, Alice", row.GetProperty("name").GetString());
    }

    // =========================================================================
    // ToonCsv.FromCsv(Stream)
    // =========================================================================

    [Fact]
    public void FromCsv_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ToonCsv.FromCsv((Stream)null!));
    }

    [Fact]
    public void FromCsv_Stream_ValidInput_ReturnsValidToon()
    {
        const string csv = "id,name\n1,Alice\n2,Bob";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var toon = ToonCsv.FromCsv(ms);

        Assert.True(Toon.IsValid(toon));
    }

    [Fact]
    public void FromCsv_Stream_DecodesAllRows()
    {
        const string csv = "id,name\n1,Alice\n2,Bob";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var toon = ToonCsv.FromCsv(ms);

        Assert.Equal(2, Toon.Decode(toon).GetArrayLength());
    }

    [Fact]
    public void FromCsv_Stream_LeavesStreamOpen()
    {
        const string csv = "id,name\n1,Alice";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        ToonCsv.FromCsv(ms);

        Assert.True(ms.CanRead);
    }

    [Fact]
    public void FromCsv_Stream_WithCustomEncoding_DecodesCorrectly()
    {
        const string csv = "id,name\n1,Alice";
        using var ms = new MemoryStream(Encoding.Unicode.GetBytes(csv));

        var toon = ToonCsv.FromCsv(ms, encoding: Encoding.Unicode);
        var row = Toon.Decode(toon)[0];

        Assert.Equal("Alice", row.GetProperty("name").GetString());
    }

    // =========================================================================
    // ToonCsv.FromCsvFile
    // =========================================================================

    [Fact]
    public void FromCsvFile_NullPath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ToonCsv.FromCsvFile(null!));
    }

    [Fact]
    public void FromCsvFile_EmptyPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ToonCsv.FromCsvFile(string.Empty));
    }

    [Fact]
    public void FromCsvFile_FileNotFound_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            ToonCsv.FromCsvFile(Path.Combine(Path.GetTempPath(), "nonexistent_toon_csv.csv")));
    }

    [Fact]
    public void FromCsvFile_ValidFile_ReturnsValidToon()
    {
        var path = GetTempPath();
        File.WriteAllText(path, "id,name,role\n1,Alice,admin\n2,Bob,user");

        var toon = ToonCsv.FromCsvFile(path);

        Assert.True(Toon.IsValid(toon));
        Assert.Equal(2, Toon.Decode(toon).GetArrayLength());
    }

    [Fact]
    public void FromCsvFile_ValidFile_PreservesFieldValues()
    {
        var path = GetTempPath();
        File.WriteAllText(path, "id,name\n7,Carol");

        var toon = ToonCsv.FromCsvFile(path);
        var row = Toon.Decode(toon)[0];

        Assert.Equal(7, row.GetProperty("id").GetInt64());
        Assert.Equal("Carol", row.GetProperty("name").GetString());
    }

    // =========================================================================
    // ToonCsv.SaveAsToon
    // =========================================================================

    [Fact]
    public void SaveAsToon_NullToonPath_ThrowsArgumentNullException()
    {
        var csvPath = GetTempPath();
        File.WriteAllText(csvPath, "id,name\n1,Alice");

        Assert.Throws<ArgumentNullException>(() => ToonCsv.SaveAsToon(csvPath, null!));
    }

    [Fact]
    public void SaveAsToon_ValidFiles_CreatesToonFile()
    {
        var csvPath  = GetTempPath(".csv");
        var toonPath = GetTempPath(".toon");
        File.WriteAllText(csvPath, "id,name\n1,Alice\n2,Bob");

        ToonCsv.SaveAsToon(csvPath, toonPath);

        Assert.True(File.Exists(toonPath));
        Assert.True(Toon.IsValid(File.ReadAllText(toonPath)));
    }

    [Fact]
    public void SaveAsToon_ValidFiles_ContentMatchesFromCsvFile()
    {
        var csvPath  = GetTempPath(".csv");
        var toonPath = GetTempPath(".toon");
        File.WriteAllText(csvPath, "id,name\n1,Alice");

        ToonCsv.SaveAsToon(csvPath, toonPath);

        Assert.Equal(ToonCsv.FromCsvFile(csvPath), File.ReadAllText(toonPath));
    }

    // =========================================================================
    // ToonCsv.FromCsvAsync
    // =========================================================================

    [Fact]
    public async Task FromCsvAsync_NullPath_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => ToonCsv.FromCsvAsync(null!));
    }

    [Fact]
    public async Task FromCsvAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            ToonCsv.FromCsvAsync(Path.Combine(Path.GetTempPath(), "nonexistent_toon_csv.csv")));
    }

    [Fact]
    public async Task FromCsvAsync_ValidFile_ReturnsValidToon()
    {
        var path = GetTempPath();
        File.WriteAllText(path, "id,name,role\n1,Alice,admin\n2,Bob,user");

        var toon = await ToonCsv.FromCsvAsync(path);

        Assert.True(Toon.IsValid(toon));
        Assert.Equal(2, Toon.Decode(toon).GetArrayLength());
    }

    [Fact]
    public async Task FromCsvAsync_MatchesSyncResult()
    {
        var path = GetTempPath();
        File.WriteAllText(path, "id,name\n1,Alice\n2,Bob");

        var asyncResult = await ToonCsv.FromCsvAsync(path);
        var syncResult  = ToonCsv.FromCsvFile(path);

        Assert.Equal(syncResult, asyncResult);
    }

    [Fact]
    public async Task FromCsvAsync_WithCancellationToken_Completes()
    {
        var path = GetTempPath();
        File.WriteAllText(path, "id,name\n1,Alice");
        using var cts = new CancellationTokenSource();

        var toon = await ToonCsv.FromCsvAsync(path, cancellationToken: cts.Token);

        Assert.True(Toon.IsValid(toon));
    }

    // =========================================================================
    // ToonCsv.SaveAsToonAsync
    // =========================================================================

    [Fact]
    public async Task SaveAsToonAsync_NullToonPath_ThrowsArgumentNullException()
    {
        var csvPath = GetTempPath();
        File.WriteAllText(csvPath, "id,name\n1,Alice");

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ToonCsv.SaveAsToonAsync(csvPath, null!));
    }

    [Fact]
    public async Task SaveAsToonAsync_ValidFiles_CreatesToonFile()
    {
        var csvPath  = GetTempPath(".csv");
        var toonPath = GetTempPath(".toon");
        File.WriteAllText(csvPath, "id,name\n1,Alice\n2,Bob");

        await ToonCsv.SaveAsToonAsync(csvPath, toonPath);

        Assert.True(File.Exists(toonPath));
        Assert.True(Toon.IsValid(File.ReadAllText(toonPath)));
    }

    // =========================================================================
    // ToonCsv.ToCsv(string)
    // =========================================================================

    [Fact]
    public void ToCsv_NullToon_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ToonCsv.ToCsv(null!));
    }

    [Fact]
    public void ToCsv_EmptyToon_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ToonCsv.ToCsv(string.Empty));
    }

    [Fact]
    public void ToCsv_ValidToon_ReturnsNonEmptyCsv()
    {
        const string toon = "[2]{id,name}:\n  1,Alice\n  2,Bob";

        var csv = ToonCsv.ToCsv(toon);

        Assert.NotEmpty(csv);
    }

    [Fact]
    public void ToCsv_ContainsAllHeaders()
    {
        const string toon = "[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user";

        var csv = ToonCsv.ToCsv(toon);

        Assert.Contains("id",   csv);
        Assert.Contains("name", csv);
        Assert.Contains("role", csv);
    }

    [Fact]
    public void ToCsv_ContainsAllDataValues()
    {
        const string toon = "[2]{id,name}:\n  1,Alice\n  2,Bob";

        var csv = ToonCsv.ToCsv(toon);

        Assert.Contains("Alice", csv);
        Assert.Contains("Bob",   csv);
    }

    [Fact]
    public void ToCsv_EmptyArray_ReturnsEmptyString()
    {
        const string toon = "[0]:";

        var csv = ToonCsv.ToCsv(toon);

        Assert.Equal(string.Empty, csv);
    }

    [Fact]
    public void ToCsv_NonArrayRoot_ThrowsInvalidOperationException()
    {
        const string toon = "name: Alice\nage: 30";

        Assert.Throws<InvalidOperationException>(() => ToonCsv.ToCsv(toon));
    }

    [Fact]
    public void ToCsv_ArrayOfPrimitives_ThrowsInvalidOperationException()
    {
        const string toon = "[3]: 1,2,3";

        Assert.Throws<InvalidOperationException>(() => ToonCsv.ToCsv(toon));
    }

    [Fact]
    public void ToCsv_NumbersWrittenWithoutQuotes()
    {
        const string toon = "[1]{id,score}:\n  42,9.99";

        var csv = ToonCsv.ToCsv(toon);
        // Exact-match the data row, not a loose Contains — "9.99" is a
        // substring of the verbose "9.9900000000000002" bug this used to
        // silently pass under.
        var dataLine = csv.Split('\n')[1].Trim('\r');

        Assert.Equal("42,9.99", dataLine);
    }

    // ElementToString used element.GetRawText() for decoded numbers, which
    // reflects the decoder's internal G17-based storage rather than a
    // clean re-formatted value — decoding the clean TOON text "9.99" and
    // exporting to CSV produced "9.9900000000000002". Confirmed
    // empirically before this fix landed.
    [Fact]
    public void ToCsv_DecodedDecimalNumber_FormattedCleanlyNotVerbosely()
    {
        const string toon = "[1]{id,amount}:\n  1,9.99";

        var csv = ToonCsv.ToCsv(toon);

        Assert.DoesNotContain("9.9900000000000002", csv);
        var dataLine = csv.Split('\n')[1].Trim('\r');
        Assert.Equal("1,9.99", dataLine);
    }

    [Fact]
    public void ToCsv_BooleansWrittenAsLowercase()
    {
        const string toon = "[2]{name,active}:\n  Alice,true\n  Bob,false";

        var csv = ToonCsv.ToCsv(toon);

        Assert.Contains("true",  csv);
        Assert.Contains("false", csv);
    }

    // =========================================================================
    // ToonCsv.ToCsvStream
    // =========================================================================

    [Fact]
    public void ToCsvStream_NullToon_ThrowsArgumentNullException()
    {
        using var ms = new MemoryStream();
        Assert.Throws<ArgumentNullException>(() => ToonCsv.ToCsvStream(null!, ms));
    }

    [Fact]
    public void ToCsvStream_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ToonCsv.ToCsvStream("[1]{id}:\n  1", (Stream)null!));
    }

    [Fact]
    public void ToCsvStream_WritesContentToStream()
    {
        const string toon = "[2]{id,name}:\n  1,Alice\n  2,Bob";
        using var ms = new MemoryStream();

        ToonCsv.ToCsvStream(toon, ms);

        Assert.True(ms.Length > 0);
    }

    [Fact]
    public void ToCsvStream_LeavesStreamOpen()
    {
        const string toon = "[1]{id,name}:\n  1,Alice";
        using var ms = new MemoryStream();

        ToonCsv.ToCsvStream(toon, ms);

        Assert.True(ms.CanRead);
    }

    [Fact]
    public void ToCsvStream_ContentMatchesToCsvString()
    {
        const string toon = "[2]{id,name}:\n  1,Alice\n  2,Bob";
        using var ms = new MemoryStream();

        ToonCsv.ToCsvStream(toon, ms);

        var streamContent = Encoding.UTF8.GetString(ms.ToArray());
        var stringContent = ToonCsv.ToCsv(toon);
        Assert.Equal(stringContent, streamContent);
    }

    [Fact]
    public void ToCsvStream_WithCustomEncoding_WritesCorrectBytes()
    {
        const string toon = "[1]{id,name}:\n  1,Alice";
        using var ms = new MemoryStream();

        ToonCsv.ToCsvStream(toon, ms, encoding: Encoding.Unicode);

        var decoded = Encoding.Unicode.GetString(ms.ToArray());
        Assert.Contains("Alice", decoded);
    }

    // =========================================================================
    // ToonCsv.ToCsvFile
    // =========================================================================

    [Fact]
    public void ToCsvFile_NullPath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ToonCsv.ToCsvFile("[1]{id}:\n  1", null!));
    }

    [Fact]
    public void ToCsvFile_ValidInput_CreatesFile()
    {
        var path = GetTempPath();
        const string toon = "[2]{id,name}:\n  1,Alice\n  2,Bob";

        ToonCsv.ToCsvFile(toon, path);

        Assert.True(File.Exists(path));
    }

    [Fact]
    public void ToCsvFile_FileContentMatchesToCsvString()
    {
        var path = GetTempPath();
        const string toon = "[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user";

        ToonCsv.ToCsvFile(toon, path);

        Assert.Equal(ToonCsv.ToCsv(toon), File.ReadAllText(path));
    }

    // =========================================================================
    // ToonCsv.ToCsvAsync
    // =========================================================================

    [Fact]
    public async Task ToCsvAsync_NullPath_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ToonCsv.ToCsvAsync("[1]{id}:\n  1", null!));
    }

    [Fact]
    public async Task ToCsvAsync_ValidInput_CreatesFile()
    {
        var path = GetTempPath();
        const string toon = "[2]{id,name}:\n  1,Alice\n  2,Bob";

        await ToonCsv.ToCsvAsync(toon, path);

        Assert.True(File.Exists(path));
        Assert.Contains("Alice", File.ReadAllText(path));
    }

    [Fact]
    public async Task ToCsvAsync_MatchesSyncResult()
    {
        var path = GetTempPath();
        const string toon = "[2]{id,name}:\n  1,Alice\n  2,Bob";

        await ToonCsv.ToCsvAsync(toon, path);

        Assert.Equal(ToonCsv.ToCsv(toon), File.ReadAllText(path));
    }

    [Fact]
    public async Task ToCsvAsync_WithCancellationToken_Completes()
    {
        var path = GetTempPath();
        const string toon = "[1]{id,name}:\n  1,Alice";
        using var cts = new CancellationTokenSource();

        await ToonCsv.ToCsvAsync(toon, path, cancellationToken: cts.Token);

        Assert.True(File.Exists(path));
    }

    // =========================================================================
    // ToonCsv.ConvertToonToCsv
    // =========================================================================

    [Fact]
    public void ConvertToonToCsv_NullToonPath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ToonCsv.ConvertToonToCsv(null!, GetTempPath()));
    }

    [Fact]
    public void ConvertToonToCsv_NullCsvPath_ThrowsArgumentNullException()
    {
        var toonPath = GetTempPath(".toon");
        File.WriteAllText(toonPath, "[1]{id}:\n  1");

        Assert.Throws<ArgumentNullException>(() => ToonCsv.ConvertToonToCsv(toonPath, null!));
    }

    [Fact]
    public void ConvertToonToCsv_ToonFileNotFound_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            ToonCsv.ConvertToonToCsv(
                Path.Combine(Path.GetTempPath(), "nonexistent_toon.toon"),
                GetTempPath()));
    }

    [Fact]
    public void ConvertToonToCsv_ValidFiles_CreatesCsvFile()
    {
        var toonPath = GetTempPath(".toon");
        var csvPath  = GetTempPath(".csv");
        const string toon = "[2]{id,name}:\n  1,Alice\n  2,Bob";
        File.WriteAllText(toonPath, toon);

        ToonCsv.ConvertToonToCsv(toonPath, csvPath);

        Assert.True(File.Exists(csvPath));
        Assert.Contains("Alice", File.ReadAllText(csvPath));
    }

    [Fact]
    public void ConvertToonToCsv_ContentMatchesToCsvFile()
    {
        var toonPath = GetTempPath(".toon");
        var csvPath  = GetTempPath(".csv");
        const string toon = "[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user";
        File.WriteAllText(toonPath, toon);

        ToonCsv.ConvertToonToCsv(toonPath, csvPath);

        Assert.Equal(ToonCsv.ToCsv(toon), File.ReadAllText(csvPath));
    }

    // =========================================================================
    // Round-trips
    // =========================================================================

    [Fact]
    public void RoundTrip_CsvToToonToCsv_PreservesRowCount()
    {
        const string original = "id,name,role\n1,Alice,admin\n2,Bob,user\n3,Carol,moderator";

        var toon = ToonCsv.FromCsv(original);
        var csv  = ToonCsv.ToCsv(toon);
        var toon2 = ToonCsv.FromCsv(csv);

        Assert.Equal(3, Toon.Decode(toon2).GetArrayLength());
    }

    [Fact]
    public void RoundTrip_CsvToToonToCsv_PreservesFieldValues()
    {
        const string original = "id,name,role\n1,Alice,admin";

        var toon    = ToonCsv.FromCsv(original);
        var csv     = ToonCsv.ToCsv(toon);
        var toon2   = ToonCsv.FromCsv(csv);
        var row     = Toon.Decode(toon2)[0];

        Assert.Equal("Alice", row.GetProperty("name").GetString());
        Assert.Equal("admin", row.GetProperty("role").GetString());
    }

    [Fact]
    public void RoundTrip_ToonToCsvToToon_ProducesSameToon()
    {
        const string toon = "[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user";

        var csv   = ToonCsv.ToCsv(toon);
        var toon2 = ToonCsv.FromCsv(csv);

        Assert.Equal(toon, toon2);
    }

    [Fact]
    public async Task RoundTrip_FileToToonToFile_PreservesData()
    {
        var csvIn  = GetTempPath(".csv");
        var toon   = GetTempPath(".toon");
        var csvOut = GetTempPath(".csv");
        File.WriteAllText(csvIn, "id,name\n1,Alice\n2,Bob");

        await ToonCsv.SaveAsToonAsync(csvIn, toon);
        ToonCsv.ConvertToonToCsv(toon, csvOut);

        var element = Toon.Decode(ToonCsv.FromCsvFile(csvOut));
        Assert.Equal(2, element.GetArrayLength());
        Assert.Equal("Alice", element[0].GetProperty("name").GetString());
    }

    // =========================================================================
    // Extension methods
    // =========================================================================

    [Fact]
    public void Extension_CsvToToon_ReturnsValidToon()
    {
        const string csv = "id,name\n1,Alice\n2,Bob";

        var toon = csv.CsvToToon();

        Assert.True(Toon.IsValid(toon));
        Assert.Equal(2, Toon.Decode(toon).GetArrayLength());
    }

    [Fact]
    public void Extension_CsvToToon_MatchesStaticMethod()
    {
        const string csv = "id,name,role\n1,Alice,admin";

        Assert.Equal(ToonCsv.FromCsv(csv), csv.CsvToToon());
    }

    [Fact]
    public void Extension_ToonToCsv_ReturnsNonEmptyString()
    {
        const string toon = "[2]{id,name}:\n  1,Alice\n  2,Bob";

        var csv = toon.ToonToCsv();

        Assert.NotEmpty(csv);
        Assert.Contains("Alice", csv);
    }

    [Fact]
    public void Extension_ToonToCsv_MatchesStaticMethod()
    {
        const string toon = "[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user";

        Assert.Equal(ToonCsv.ToCsv(toon), toon.ToonToCsv());
    }

    [Fact]
    public void Extension_CsvToToon_WithOptions_PassesThrough()
    {
        const string csv = "id,name\n1,Alice";
        var opts = new EncodeOptions { Delimiter = '|' };

        var toon = csv.CsvToToon(opts);

        Assert.Contains("|", toon);
    }

    // =========================================================================
    // Multi-dataset: TOON -> multiple CSVs. CSV has no native multi-table
    // concept, so — mirroring Toon.DotNet.Excel's "one worksheet per
    // top-level key" — the equivalent here is "one CSV per top-level key."
    // ToCsvDictionary/ToCsvFiles require the TOON root to be an OBJECT
    // (one array per key); a root ARRAY (a single, unnamed dataset) is out
    // of scope for these and should use the existing ToCsv/FromCsv methods
    // instead — this keeps API responsibilities crisply separated rather
    // than inventing a default key name for the single-dataset case.
    // =========================================================================

    [Fact]
    public void ToCsvDictionary_MultiDatasetToon_ReturnsOneEntryPerKey()
    {
        const string toon = "Sales[1]{id,amount}:\n  1,9.99\nCustomers[1]{id,name}:\n  1,Alice";

        var result = ToonCsv.ToCsvDictionary(toon);

        Assert.Equal(2, result.Count);
        Assert.Contains("id,amount", result["Sales"]);
        Assert.Contains("1,9.99", result["Sales"]);
        Assert.Contains("id,name", result["Customers"]);
        Assert.Contains("1,Alice", result["Customers"]);
    }

    [Fact]
    public void ToCsvDictionary_RootArray_Throws()
    {
        const string toon = "[2]{id,name}:\n  1,Alice\n  2,Bob";

        var ex = Assert.Throws<InvalidOperationException>(() => ToonCsv.ToCsvDictionary(toon));
        Assert.Contains("ToCsv", ex.Message);
    }

    [Fact]
    public void ToCsvDictionary_NonArrayTopLevelValue_ThrowsNamingKey()
    {
        const string toon = "Sales[1]{id,amount}:\n  1,9.99\nConfig:\n  theme: dark";

        var ex = Assert.Throws<InvalidOperationException>(() => ToonCsv.ToCsvDictionary(toon));
        Assert.Contains("Config", ex.Message);
    }

    [Fact]
    public void ToCsvDictionary_NullToon_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ToonCsv.ToCsvDictionary(null!));
    }

    [Fact]
    public void ToCsvDictionary_EmptyStringToon_ThrowsArgumentException()
    {
        // Matches ToCsv's own convention: this CSV-package boundary
        // rejects empty input even though core Toon.Decode("") itself
        // is spec-valid (§5: an empty document decodes to {}).
        Assert.Throws<ArgumentException>(() => ToonCsv.ToCsvDictionary(""));
    }

    [Fact]
    public void ToCsvDictionary_ToonThatDecodesToEmptyObject_ReturnsEmptyDictionary()
    {
        // Non-empty input string (a comment-only document) that decodes
        // to a genuinely empty object — distinct from the rejected empty
        // string case above.
        var result = ToonCsv.ToCsvDictionary("# just a comment");

        Assert.Empty(result);
    }

    // =========================================================================
    // Multi-dataset: TOON -> CSV files
    // =========================================================================

    [Fact]
    public void ToCsvFiles_MultiDatasetToon_WritesOneFilePerKey()
    {
        const string toon = "Sales[1]{id,amount}:\n  1,9.99\nCustomers[1]{id,name}:\n  1,Alice";
        var dir = GetTempDirectory();

        var written = ToonCsv.ToCsvFiles(toon, dir);

        Assert.Equal(2, written.Count);
        Assert.True(File.Exists(written["Sales"]));
        Assert.True(File.Exists(written["Customers"]));
        Assert.Contains("1,9.99", File.ReadAllText(written["Sales"]));
        Assert.Contains("1,Alice", File.ReadAllText(written["Customers"]));
        Assert.Equal("Sales.csv", Path.GetFileName(written["Sales"]));
        Assert.Equal("Customers.csv", Path.GetFileName(written["Customers"]));
    }

    [Fact]
    public void ToCsvFiles_CreatesOutputDirectoryIfMissing()
    {
        const string toon = "Sales[1]{id,amount}:\n  1,9.99";
        var dir = Path.Combine(GetTempDirectory(), "nested", "subdir");

        Assert.False(Directory.Exists(dir));

        ToonCsv.ToCsvFiles(toon, dir);

        Assert.True(Directory.Exists(dir));
    }

    [Fact]
    public void ToCsvFiles_KeyWithInvalidFileNameChars_IsSanitized()
    {
        const string toon = "\"Sales/Data\"[1]{id}:\n  1";
        var dir = GetTempDirectory();

        var written = ToonCsv.ToCsvFiles(toon, dir);

        var path = Assert.Single(written).Value;
        Assert.DoesNotContain('/', Path.GetFileName(path));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void ToCsvFiles_KeysCollidingAfterSanitization_DedupsWithSuffix()
    {
        // Both keys sanitize to the same base file name ("SalesData").
        const string toon = "\"Sales/Data\"[1]{id}:\n  1\n\"Sales?Data\"[1]{id}:\n  2";
        var dir = GetTempDirectory();

        var written = ToonCsv.ToCsvFiles(toon, dir);

        Assert.Equal(2, written.Count);
        var fileNames = written.Values.Select(Path.GetFileName).ToHashSet();
        Assert.Equal(2, fileNames.Count);
        Assert.True(File.Exists(written["Sales/Data"]));
        Assert.True(File.Exists(written["Sales?Data"]));
    }

    [Fact]
    public void ToCsvFiles_NullOutputDirectory_ThrowsArgumentNullException()
    {
        const string toon = "Sales[1]{id}:\n  1";

        Assert.Throws<ArgumentNullException>(() => ToonCsv.ToCsvFiles(toon, null!));
    }

    [Fact]
    public void ToCsvFiles_EmptyOutputDirectory_ThrowsArgumentException()
    {
        const string toon = "Sales[1]{id}:\n  1";

        Assert.Throws<ArgumentException>(() => ToonCsv.ToCsvFiles(toon, ""));
    }

    [Fact]
    public async Task ToCsvFilesAsync_MultiDatasetToon_WritesOneFilePerKey()
    {
        const string toon = "Sales[1]{id,amount}:\n  1,9.99\nCustomers[1]{id,name}:\n  1,Alice";
        var dir = GetTempDirectory();

        var written = await ToonCsv.ToCsvFilesAsync(toon, dir);

        Assert.Equal(2, written.Count);
        Assert.Contains("1,9.99", await File.ReadAllTextAsync(written["Sales"]));
        Assert.Contains("1,Alice", await File.ReadAllTextAsync(written["Customers"]));
    }

    // =========================================================================
    // Multi-dataset: multiple CSVs -> TOON (the reverse direction)
    // =========================================================================

    [Fact]
    public void FromCsvDictionary_MultipleEntries_ProducesMultiDatasetToon()
    {
        var csvByName = new Dictionary<string, string>
        {
            ["Sales"] = "id,amount\n1,9.99",
            ["Customers"] = "id,name\n1,Alice",
        };

        var toon = ToonCsv.FromCsvDictionary(csvByName);
        var result = Toon.Decode(toon);

        Assert.Equal(9.99, result.GetProperty("Sales")[0].GetProperty("amount").GetDouble());
        Assert.Equal("Alice", result.GetProperty("Customers")[0].GetProperty("name").GetString());
    }

    [Fact]
    public void FromCsvDictionary_SingleEntry_StillProducesRootObject_NotUnwrapped()
    {
        var csvByName = new Dictionary<string, string> { ["data"] = "id,name\n1,Alice" };

        var toon = ToonCsv.FromCsvDictionary(csvByName);
        var result = Toon.Decode(toon);

        Assert.Equal(JsonValueKind.Object, result.ValueKind);
        Assert.Equal("Alice", result.GetProperty("data")[0].GetProperty("name").GetString());
    }

    [Fact]
    public void FromCsvDictionary_NullDictionary_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ToonCsv.FromCsvDictionary(null!));
    }

    [Fact]
    public void FromCsvDictionary_EmptyDictionary_ProducesEmptyObject()
    {
        var toon = ToonCsv.FromCsvDictionary(new Dictionary<string, string>());
        var result = Toon.Decode(toon);

        Assert.Equal(JsonValueKind.Object, result.ValueKind);
        Assert.Empty(result.EnumerateObject().ToArray());
    }

    [Fact]
    public void FromCsvFiles_DirectoryOfCsvFiles_ProducesMultiDatasetToon()
    {
        var dir = GetTempDirectory();
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Sales.csv"), "id,amount\n1,9.99");
        File.WriteAllText(Path.Combine(dir, "Customers.csv"), "id,name\n1,Alice");

        var toon = ToonCsv.FromCsvFiles(dir);
        var result = Toon.Decode(toon);

        Assert.Equal(9.99, result.GetProperty("Sales")[0].GetProperty("amount").GetDouble());
        Assert.Equal("Alice", result.GetProperty("Customers")[0].GetProperty("name").GetString());
    }

    [Fact]
    public void FromCsvFiles_NonexistentDirectory_ThrowsDirectoryNotFoundException()
    {
        var dir = GetTempDirectory();

        Assert.Throws<DirectoryNotFoundException>(() => ToonCsv.FromCsvFiles(dir));
    }

    [Fact]
    public void FromCsvFiles_EmptyDirectoryOrNoMatches_ReturnsEmptyObjectToon()
    {
        var dir = GetTempDirectory();
        Directory.CreateDirectory(dir);

        var toon = ToonCsv.FromCsvFiles(dir);
        var result = Toon.Decode(toon);

        Assert.Equal(JsonValueKind.Object, result.ValueKind);
        Assert.Empty(result.EnumerateObject().ToArray());
    }

    [Fact]
    public void FromCsvFiles_NullDirectory_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ToonCsv.FromCsvFiles(null!));
    }

    [Fact]
    public void FromCsvFiles_EmptyDirectory_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ToonCsv.FromCsvFiles(""));
    }

    [Fact]
    public void FromCsvFiles_CustomSearchPattern_OnlyMatchesPattern()
    {
        var dir = GetTempDirectory();
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Sales.csv"), "id,amount\n1,9.99");
        File.WriteAllText(Path.Combine(dir, "notes.txt"), "not csv");

        var toon = ToonCsv.FromCsvFiles(dir);
        var result = Toon.Decode(toon);

        Assert.Single(result.EnumerateObject().ToArray());
        Assert.True(result.TryGetProperty("Sales", out _));
    }

    [Fact]
    public async Task FromCsvFilesAsync_DirectoryOfCsvFiles_ProducesMultiDatasetToon()
    {
        var dir = GetTempDirectory();
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Sales.csv"), "id,amount\n1,9.99");

        var toon = await ToonCsv.FromCsvFilesAsync(dir);
        var result = Toon.Decode(toon);

        Assert.Equal(9.99, result.GetProperty("Sales")[0].GetProperty("amount").GetDouble());
    }

    [Fact]
    public void SaveCsvFilesAsToon_WritesToonFile()
    {
        var dir = GetTempDirectory();
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Sales.csv"), "id,amount\n1,9.99");
        var toonPath = GetTempPath(".toon");

        ToonCsv.SaveCsvFilesAsToon(dir, toonPath);

        Assert.True(File.Exists(toonPath));
        var result = Toon.Decode(File.ReadAllText(toonPath));
        Assert.Equal(9.99, result.GetProperty("Sales")[0].GetProperty("amount").GetDouble());
    }

    [Fact]
    public void RoundTrip_ToCsvFilesThenFromCsvFiles_PreservesDatasetContent()
    {
        const string toon = "Sales[1]{id,amount}:\n  1,9.99\nCustomers[1]{id,name}:\n  1,Alice";
        var dir = GetTempDirectory();

        ToonCsv.ToCsvFiles(toon, dir);
        var roundTripped = ToonCsv.FromCsvFiles(dir);
        var result = Toon.Decode(roundTripped);

        Assert.Equal(9.99, result.GetProperty("Sales")[0].GetProperty("amount").GetDouble());
        Assert.Equal("Alice", result.GetProperty("Customers")[0].GetProperty("name").GetString());
    }
}
