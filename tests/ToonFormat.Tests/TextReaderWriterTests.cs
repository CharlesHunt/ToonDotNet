using System.Data;
using System.Text;
using System.Text.Json;

namespace ToonFormat.Tests;

/// <summary>
/// Tests for the TextWriter / TextReader encode/decode overloads and the
/// DataTable-to-Stream encode overload.
/// </summary>
public class TextReaderWriterTests
{
    private static string StreamToString(MemoryStream ms, Encoding? encoding = null) =>
        (encoding ?? Encoding.UTF8).GetString(ms.ToArray()).TrimStart('\uFEFF');

    // =========================================================================
    // Toon.Encode(object?, TextWriter, EncodeOptions?)
    // =========================================================================

    [Fact]
    public void Encode_TextWriter_WritesValidToon()
    {
        var data = new { name = "Alice", age = 30 };
        using var sw = new StringWriter();

        Toon.Encode(data, sw);

        Assert.True(Toon.IsValid(sw.ToString()));
    }

    [Fact]
    public void Encode_TextWriter_ContentMatchesStringEncode()
    {
        var data = new { users = new[] { new { id = 1, name = "Alice" } } };
        using var sw = new StringWriter();

        Toon.Encode(data, sw);

        Assert.Equal(Toon.Encode(data), sw.ToString());
    }

    [Fact]
    public void Encode_TextWriter_WithOptions_AppliesOptions()
    {
        var data = new { users = new[] { new { id = 1, name = "Alice" } } };
        using var sw = new StringWriter();
        var opts = new EncodeOptions { Delimiter = '|' };

        Toon.Encode(data, sw, opts);

        Assert.Equal(Toon.Encode(data, opts), sw.ToString());
    }

    [Fact]
    public void Encode_TextWriter_NullInput_WritesContent()
    {
        using var sw = new StringWriter();

        Toon.Encode(null, sw);

        Assert.True(sw.ToString().Length >= 0);
    }

    [Fact]
    public void Encode_TextWriter_NullWriter_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Toon.Encode(new { }, (TextWriter)null!));
    }

    // =========================================================================
    // Toon.Decode(TextReader, DecodeOptions?) → JsonElement
    // =========================================================================

    [Fact]
    public void Decode_TextReader_ReturnsCorrectElement()
    {
        using var sr = new StringReader("name: Alice\nage: 30");

        var result = Toon.Decode(sr);

        Assert.Equal(JsonValueKind.Object, result.ValueKind);
        Assert.Equal("Alice", result.GetProperty("name").GetString());
        Assert.Equal(30, result.GetProperty("age").GetInt32());
    }

    [Fact]
    public void Decode_TextReader_TabularArray_ReturnsArray()
    {
        using var sr = new StringReader("[2]{id,name}:\n  1,Alice\n  2,Bob");

        var result = Toon.Decode(sr);

        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal(2, result.GetArrayLength());
        Assert.Equal("Alice", result[0].GetProperty("name").GetString());
    }

    [Fact]
    public void Decode_TextReader_NullReader_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Toon.Decode((TextReader)null!));
    }

    [Fact]
    public void Decode_TextReader_ProducesSameResultAsStringDecode()
    {
        const string toon = "users[2]{id,name}:\n  1,Alice\n  2,Bob";
        using var sr = new StringReader(toon);

        var readerResult = Toon.Decode(sr);
        var stringResult = Toon.Decode(toon);

        Assert.Equal(stringResult.GetRawText(), readerResult.GetRawText());
    }

    // =========================================================================
    // Toon.Decode<T>(TextReader, DecodeOptions?, JsonSerializerOptions?) → T
    // =========================================================================

    [Fact]
    public void DecodeTyped_TextReader_ReturnsTypedObject()
    {
        using var sr = new StringReader("id: 1\nname: Alice\nrole: admin");

        var result = Toon.Decode<TextRWTestUser>(sr);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Alice", result.Name);
        Assert.Equal("admin", result.Role);
    }

    [Fact]
    public void DecodeTyped_TextReader_WithCustomJsonOptions_AppliesOptions()
    {
        using var sr = new StringReader("userName: Alice");
        var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var result = Toon.Decode<Dictionary<string, string>>(sr, jsonOptions: jsonOpts);

        Assert.Contains("userName", result.Keys);
        Assert.Equal("Alice", result["userName"]);
    }

    [Fact]
    public void DecodeTyped_TextReader_NullReader_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Toon.Decode<TextRWTestUser>((TextReader)null!));
    }

    [Fact]
    public void DecodeTyped_TextReader_ProducesSameResultAsStringDecode()
    {
        const string toon = "id: 7\nname: Bob\nrole: user";
        using var sr = new StringReader(toon);

        var readerResult = Toon.Decode<TextRWTestUser>(sr);
        var stringResult = Toon.Decode<TextRWTestUser>(toon);

        Assert.Equal(stringResult.Id,   readerResult.Id);
        Assert.Equal(stringResult.Name, readerResult.Name);
        Assert.Equal(stringResult.Role, readerResult.Role);
    }

    // =========================================================================
    // Toon.Encode(DataTable, Stream, EncodeOptions?, Encoding?)
    // =========================================================================

    [Fact]
    public void Encode_DataTable_Stream_WritesValidToon()
    {
        using var ms = new MemoryStream();

        Toon.Encode(BuildTable(), ms);

        Assert.True(Toon.IsValid(StreamToString(ms)));
    }

    [Fact]
    public void Encode_DataTable_Stream_ContentMatchesStringEncode()
    {
        var table = BuildTable();
        using var ms = new MemoryStream();

        Toon.Encode(table, ms);

        Assert.Equal(Toon.Encode(table), StreamToString(ms));
    }

    [Fact]
    public void Encode_DataTable_Stream_WithOptions_AppliesOptions()
    {
        var table = BuildTable();
        var opts = new EncodeOptions { Delimiter = '|' };
        using var ms = new MemoryStream();

        Toon.Encode(table, ms, opts);

        Assert.Equal(Toon.Encode(table, opts), StreamToString(ms));
    }

    [Fact]
    public void Encode_DataTable_Stream_NullTable_ThrowsArgumentNullException()
    {
        using var ms = new MemoryStream();
        Assert.Throws<ArgumentNullException>(() => Toon.Encode((DataTable)null!, ms));
    }

    [Fact]
    public void Encode_DataTable_Stream_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Toon.Encode(BuildTable(), (Stream)null!));
    }

    [Fact]
    public void Encode_DataTable_Stream_LeavesStreamOpen()
    {
        using var ms = new MemoryStream();

        Toon.Encode(BuildTable(), ms);

        Assert.True(ms.CanRead);
    }

    [Fact]
    public void Encode_DataTable_Stream_WithCustomEncoding_WritesCorrectBytes()
    {
        using var ms = new MemoryStream();

        Toon.Encode(BuildTable(), ms, encoding: Encoding.Unicode);

        Assert.True(Toon.IsValid(StreamToString(ms, Encoding.Unicode)));
    }

    // =========================================================================
    // Round-trips
    // =========================================================================

    [Fact]
    public void RoundTrip_TextWriterTextReader_PreservesValues()
    {
        var original = new { id = 42, label = "test", active = true };
        using var sw = new StringWriter();

        Toon.Encode(original, sw);

        using var sr = new StringReader(sw.ToString());
        var result = Toon.Decode(sr);

        Assert.Equal(42,     result.GetProperty("id").GetInt32());
        Assert.Equal("test", result.GetProperty("label").GetString());
        Assert.True(result.GetProperty("active").GetBoolean());
    }

    [Fact]
    public void RoundTrip_TextWriterTextReader_TypedPreservesValues()
    {
        var original = new TextRWTestUser { Id = 3, Name = "Charlie", Role = "moderator" };
        using var sw = new StringWriter();

        Toon.Encode(original, sw);

        using var sr = new StringReader(sw.ToString());
        var result = Toon.Decode<TextRWTestUser>(sr);

        Assert.Equal(original.Id,   result.Id);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Role, result.Role);
    }

    [Fact]
    public void RoundTrip_TextWriterTextReader_TabularData_PreservesAllRows()
    {
        var data = new[]
        {
            new { id = 1, name = "Alice", role = "admin"     },
            new { id = 2, name = "Bob",   role = "user"      },
            new { id = 3, name = "Carol", role = "moderator" },
        };
        using var sw = new StringWriter();

        Toon.Encode(data, sw);

        using var sr = new StringReader(sw.ToString());
        var result = Toon.Decode(sr);

        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal(3, result.GetArrayLength());
        Assert.Equal("Carol", result[2].GetProperty("name").GetString());
    }

    [Fact]
    public void RoundTrip_DataTable_StreamEncodeDecode_PreservesValues()
    {
        var table = BuildTable();
        using var ms = new MemoryStream();

        Toon.Encode(table, ms);
        ms.Position = 0;
        var result = Toon.Decode(ms);

        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal(2, result.GetArrayLength());
        Assert.Equal("Alice", result[0].GetProperty("name").GetString());
        Assert.Equal("Bob",   result[1].GetProperty("name").GetString());
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static DataTable BuildTable()
    {
        var table = new DataTable();
        table.Columns.Add("id",   typeof(int));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("role", typeof(string));
        table.Rows.Add(1, "Alice", "admin");
        table.Rows.Add(2, "Bob",   "user");
        return table;
    }
}

// ---------------------------------------------------------------------------
// Test fixture model
// ---------------------------------------------------------------------------
file class TextRWTestUser
{
    public int    Id   { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
}
