using System.Text.Json;

namespace ToonFormat.Tests;

public class ToonDebuggingTests
{
    // Previously named Debug_InvalidSyntax_ShouldFail and asserted via a
    // try/catch where Assert.True(false, ...) inside the try block threw
    // an exception that the very next catch block swallowed and turned
    // into a passing assertion — the test passed unconditionally regardless
    // of what Decode actually did. It also used "invalid [[ syntax" as the
    // "invalid" input, which is actually valid TOON: spec §5 decodes a
    // single line that's neither an array header nor a key-value pair as a
    // plain string primitive. Replaced with genuinely malformed input
    // (spec §6: a non-numeric array-length header) and a direct
    // Assert.Throws.
    [Fact]
    public void Decode_MalformedArrayHeader_ThrowsException()
    {
        var malformedToon = "items[abc]: value";

        Assert.Throws<InvalidOperationException>(() => Toon.Decode(malformedToon));
    }

    [Fact]
    public void Decode_SingleNonKeyValueLine_DecodesAsStringPrimitive()
    {
        // The input the old test used, decoding correctly per spec §5.
        var result = Toon.Decode("invalid [[ syntax");

        Assert.Equal(JsonValueKind.String, result.ValueKind);
        Assert.Equal("invalid [[ syntax", result.GetString());
    }
}
