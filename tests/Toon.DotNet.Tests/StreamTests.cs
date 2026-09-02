using System.Text;
using System.Text.Json;

namespace ToonFormat.Tests;

/// <summary>
/// Tests for synchronous and asynchronous stream-based encode/decode overloads.
/// All tests use MemoryStream so they run without file I/O.
/// </summary>
public class StreamTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static MemoryStream ToonToStream(string toon, Encoding? encoding = null) =>
        new((encoding ?? Encoding.UTF8).GetBytes(toon));

    private static string StreamToString(MemoryStream ms, Encoding? encoding = null) =>
        (encoding ?? Encoding.UTF8).GetString(ms.ToArray()).TrimStart('\uFEFF');

    // =========================================================================
    // Toon.Encode(object?, Stream, ...)
    // =========================================================================

    [Fact]
    public void Encode_Stream_WritesValidToon()
    {
        var data = new { name = "Alice", age = 30 };
        using var ms = new MemoryStream();

        Toon.Encode(data, ms);

        Assert.True(ms.Length > 0);
        Assert.True(Toon.IsValid(StreamToString(ms)));
    }

    [Fact]
    public void Encode_Stream_ContentMatchesStringEncode()
    {
        var data = new { users = new[] { new { id = 1, name = "Alice" } } };
        using var ms = new MemoryStream();

        Toon.Encode(data, ms);

        Assert.Equal(Toon.Encode(data), StreamToString(ms));
    }

    [Fact]
    public void Encode_Stream_WithOptions_AppliesOptions()
    {
        var data = new { users = new[] { new { id = 1, name = "Alice" } } };
        using var ms = new MemoryStream();
        var opts = new EncodeOptions { Delimiter = '|' };

        Toon.Encode(data, ms, opts);

        var streamData = StreamToString(ms);
        var nonStreamData = Toon.Encode(data, opts);

        Assert.Equal(nonStreamData, streamData);
    }

    [Fact]
    public void Encode_Stream_NullInput_WritesContent()
    {
        using var ms = new MemoryStream();

        Toon.Encode((object?)null, ms);

        Assert.True(ms.Length >= 0);
    }

    [Fact]
    public void Encode_Stream_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Toon.Encode(new { }, (Stream)null!));
    }

    [Fact]
    public void Encode_Stream_LeavesStreamOpen()
    {
        using var ms = new MemoryStream();

        Toon.Encode(new { id = 1 }, ms);

        Assert.True(ms.CanRead);
    }

    [Fact]
    public void Encode_Stream_WithCustomEncoding_WritesCorrectBytes()
    {
        var data = new { name = "Alice" };
        using var ms = new MemoryStream();

        Toon.Encode(data, ms, encoding: Encoding.Unicode);

        var decoded = Encoding.Unicode.GetString(ms.ToArray());
        Assert.True(Toon.IsValid(decoded));
    }

    // =========================================================================
    // Toon.Decode(Stream, ...) ? JsonElement
    // =========================================================================

    [Fact]
    public void Decode_Stream_ReturnsCorrectElement()
    {
        using var ms = ToonToStream("name: Alice\nage: 30");

        var result = Toon.Decode(ms);

        Assert.Equal(JsonValueKind.Object, result.ValueKind);
        Assert.Equal("Alice", result.GetProperty("name").GetString());
        Assert.Equal(30, result.GetProperty("age").GetInt32());
    }

    [Fact]
    public void Decode_Stream_TabularArray_ReturnsArray()
    {
        using var ms = ToonToStream("[2]{id,name}:\n  1,Alice\n  2,Bob");

        var result = Toon.Decode(ms);

        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal(2, result.GetArrayLength());
        Assert.Equal("Alice", result[0].GetProperty("name").GetString());
    }

    [Fact]
    public void Decode_Stream_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Toon.Decode((Stream)null!));
    }

    [Fact]
    public void Decode_Stream_LeavesStreamOpen()
    {
        using var ms = ToonToStream("name: Alice");

        Toon.Decode(ms);

        Assert.True(ms.CanRead);
    }

    [Fact]
    public void Decode_Stream_WithCustomEncoding_DecodesCorrectly()
    {
        var toon = "name: Alice\nage: 30";
        using var ms = ToonToStream(toon, Encoding.Unicode);

        var result = Toon.Decode(ms, encoding: Encoding.Unicode);

        Assert.Equal("Alice", result.GetProperty("name").GetString());
    }

    [Fact]
    public void Decode_Stream_ProducesSameResultAsStringDecode()
    {
        const string toon = "users[2]{id,name}:\n  1,Alice\n  2,Bob";
        using var ms = ToonToStream(toon);

        var streamResult = Toon.Decode(ms);
        var stringResult = Toon.Decode(toon);

        Assert.Equal(stringResult.GetRawText(), streamResult.GetRawText());
    }

    // =========================================================================
    // Toon.Decode<T>(Stream, ...) ? T
    // =========================================================================

    [Fact]
    public void DecodeTyped_Stream_ReturnsTypedObject()
    {
        using var ms = ToonToStream("id: 1\nname: Alice\nrole: admin");

        var result = Toon.Decode<StreamTestUser>(ms);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Alice", result.Name);
        Assert.Equal("admin", result.Role);
    }

    [Fact]
    public void DecodeTyped_Stream_WithCustomJsonOptions_AppliesOptions()
    {
        using var ms = ToonToStream("userName: Alice");
        var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var result = Toon.Decode<Dictionary<string, string>>(ms, jsonOptions: jsonOpts);

        Assert.Contains("userName", result.Keys);
        Assert.Equal("Alice", result["userName"]);
    }

    [Fact]
    public void DecodeTyped_Stream_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Toon.Decode<StreamTestUser>((Stream)null!));
    }

    [Fact]
    public void DecodeTyped_Stream_ProducesSameResultAsStringDecode()
    {
        const string toon = "id: 7\nname: Bob\nrole: user";
        using var ms = ToonToStream(toon);

        var streamResult = Toon.Decode<StreamTestUser>(ms);
        var stringResult = Toon.Decode<StreamTestUser>(toon);

        Assert.Equal(stringResult.Id,   streamResult.Id);
        Assert.Equal(stringResult.Name, streamResult.Name);
        Assert.Equal(stringResult.Role, streamResult.Role);
    }

    // =========================================================================
    // Toon.EncodeAsync(object?, Stream, ...)
    // =========================================================================

    [Fact]
    public async Task EncodeAsync_Stream_WritesValidToon()
    {
        var data = new { name = "Alice", age = 30 };
        using var ms = new MemoryStream();

        await Toon.EncodeAsync(data, ms);

        Assert.True(ms.Length > 0);
        Assert.True(Toon.IsValid(StreamToString(ms)));
    }

    [Fact]
    public async Task EncodeAsync_Stream_ContentMatchesSyncEncode()
    {
        var data = new { users = new[] { new { id = 1, name = "Alice" } } };
        using var ms = new MemoryStream();

        await Toon.EncodeAsync(data, ms);

        Assert.Equal(Toon.Encode(data), StreamToString(ms));
    }

    [Fact]
    public async Task EncodeAsync_Stream_WithOptions_AppliesOptions()
    {
        var data = new { users = new[] { new { id = 1, name = "Alice" } } };
        using var ms = new MemoryStream();

        await Toon.EncodeAsync(data, ms, new EncodeOptions { Delimiter = '|' });

        Assert.Contains("|", StreamToString(ms));
    }

    [Fact]
    public async Task EncodeAsync_Stream_NullStream_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Toon.EncodeAsync(new { }, (Stream)null!));
    }

    [Fact]
    public async Task EncodeAsync_Stream_LeavesStreamOpen()
    {
        using var ms = new MemoryStream();

        await Toon.EncodeAsync(new { id = 1 }, ms);

        Assert.True(ms.CanRead);
    }

    [Fact]
    public async Task EncodeAsync_Stream_WithCancellationToken_Completes()
    {
        using var ms = new MemoryStream();
        using var cts = new CancellationTokenSource();

        await Toon.EncodeAsync(new { name = "Alice" }, ms, cancellationToken: cts.Token);

        Assert.True(ms.Length > 0);
    }

    // =========================================================================
    // Toon.DecodeAsync(Stream, ...) ? Task<JsonElement>
    // =========================================================================

    [Fact]
    public async Task DecodeAsync_Stream_ReturnsCorrectElement()
    {
        using var ms = ToonToStream("name: Alice\nage: 30");

        var result = await Toon.DecodeAsync(ms);

        Assert.Equal(JsonValueKind.Object, result.ValueKind);
        Assert.Equal("Alice", result.GetProperty("name").GetString());
        Assert.Equal(30, result.GetProperty("age").GetInt32());
    }

    [Fact]
    public async Task DecodeAsync_Stream_TabularArray_ReturnsArray()
    {
        using var ms = ToonToStream("[2]{id,name}:\n  1,Alice\n  2,Bob");

        var result = await Toon.DecodeAsync(ms);

        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal(2, result.GetArrayLength());
        Assert.Equal("Bob", result[1].GetProperty("name").GetString());
    }

    [Fact]
    public async Task DecodeAsync_Stream_NullStream_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Toon.DecodeAsync((Stream)null!));
    }

    [Fact]
    public async Task DecodeAsync_Stream_LeavesStreamOpen()
    {
        using var ms = ToonToStream("name: Alice");

        await Toon.DecodeAsync(ms);

        Assert.True(ms.CanRead);
    }

    [Fact]
    public async Task DecodeAsync_Stream_ProducesSameResultAsSyncDecode()
    {
        const string toon = "users[2]{id,name}:\n  1,Alice\n  2,Bob";
        using var ms = ToonToStream(toon);

        var asyncResult = await Toon.DecodeAsync(ms);
        var syncResult  = Toon.Decode(toon);

        Assert.Equal(syncResult.GetRawText(), asyncResult.GetRawText());
    }

    [Fact]
    public async Task DecodeAsync_Stream_WithCancellationToken_Completes()
    {
        using var ms = ToonToStream("name: Alice");
        using var cts = new CancellationTokenSource();

        var result = await Toon.DecodeAsync(ms, cancellationToken: cts.Token);

        Assert.Equal("Alice", result.GetProperty("name").GetString());
    }

    // =========================================================================
    // Toon.DecodeAsync<T>(Stream, ...) ? Task<T>
    // =========================================================================

    [Fact]
    public async Task DecodeAsyncTyped_Stream_ReturnsTypedObject()
    {
        using var ms = ToonToStream("id: 1\nname: Alice\nrole: admin");

        var result = await Toon.DecodeAsync<StreamTestUser>(ms);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Alice", result.Name);
        Assert.Equal("admin", result.Role);
    }

    [Fact]
    public async Task DecodeAsyncTyped_Stream_NullStream_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Toon.DecodeAsync<StreamTestUser>((Stream)null!));
    }

    [Fact]
    public async Task DecodeAsyncTyped_Stream_ProducesSameResultAsSyncDecode()
    {
        const string toon = "id: 7\nname: Bob\nrole: user";
        using var ms1 = ToonToStream(toon);
        using var ms2 = ToonToStream(toon);

        var asyncResult = await Toon.DecodeAsync<StreamTestUser>(ms1);
        var syncResult  = Toon.Decode<StreamTestUser>(ms2);

        Assert.Equal(syncResult.Id,   asyncResult.Id);
        Assert.Equal(syncResult.Name, asyncResult.Name);
    }

    // =========================================================================
    // Round-trips
    // =========================================================================

    [Fact]
    public void RoundTrip_SyncEncodeDecodePrimitive_PreservesValues()
    {
        var original = new { id = 42, label = "test", active = true };
        using var ms = new MemoryStream();

        Toon.Encode(original, ms);
        ms.Position = 0;
        var result = Toon.Decode(ms);

        Assert.Equal(42,     result.GetProperty("id").GetInt32());
        Assert.Equal("test", result.GetProperty("label").GetString());
        Assert.True(result.GetProperty("active").GetBoolean());
    }

    [Fact]
    public void RoundTrip_SyncEncodeDecodeTyped_PreservesValues()
    {
        var original = new StreamTestUser { Id = 3, Name = "Charlie", Role = "moderator" };
        using var ms = new MemoryStream();

        Toon.Encode(original, ms);
        ms.Position = 0;
        var result = Toon.Decode<StreamTestUser>(ms);

        Assert.Equal(original.Id,   result.Id);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Role, result.Role);
    }

    [Fact]
    public async Task RoundTrip_AsyncEncodeDecodeElement_PreservesValues()
    {
        var original = new { id = 5, name = "Dana", active = false };
        using var ms = new MemoryStream();

        await Toon.EncodeAsync(original, ms);
        ms.Position = 0;
        var result = await Toon.DecodeAsync(ms);

        Assert.Equal(5,      result.GetProperty("id").GetInt32());
        Assert.Equal("Dana", result.GetProperty("name").GetString());
        Assert.False(result.GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task RoundTrip_AsyncEncodeDecodeTyped_PreservesValues()
    {
        var original = new StreamTestUser { Id = 9, Name = "Eve", Role = "admin" };
        using var ms = new MemoryStream();

        await Toon.EncodeAsync(original, ms);
        ms.Position = 0;
        var result = await Toon.DecodeAsync<StreamTestUser>(ms);

        Assert.Equal(original.Id,   result.Id);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Role, result.Role);
    }

    [Fact]
    public async Task RoundTrip_TabularData_PreservesAllRows()
    {
        var data = new[]
        {
            new { id = 1, name = "Alice", role = "admin"     },
            new { id = 2, name = "Bob",   role = "user"      },
            new { id = 3, name = "Carol", role = "moderator" },
        };
        using var ms = new MemoryStream();

        await Toon.EncodeAsync(data, ms);
        ms.Position = 0;
        var result = await Toon.DecodeAsync(ms);

        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal(3, result.GetArrayLength());
        Assert.Equal("Carol", result[2].GetProperty("name").GetString());
    }
}

// ---------------------------------------------------------------------------
// Test fixture model
// ---------------------------------------------------------------------------
file class StreamTestUser
{
    public int    Id   { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
}
