using System.Text.Json;

namespace ToonFormat.Tests;

// Tracks gaps from TOON_V3.md's fix order against spec v3.0.x-v3.3.2.
// Fix-order items 1-5 (findings 1, 2, 4, 5, 6, 7, 8, 9, 13) are done; the
// tests below for the remaining items are added ahead of each fix, so a
// failure here going in is a confirmed gap, not a regression.
public class ToonV3ComplianceTests
{
    // Finding 1: an empty document decodes to {} (spec §5), not a thrown
    // exception.
    [Fact]
    public void Decode_EmptyString_ReturnsEmptyObject()
    {
        var result = Toon.Decode("");

        Assert.Equal(JsonValueKind.Object, result.ValueKind);
        Assert.Equal(0, result.EnumerateObject().Count());
    }

    [Fact]
    public void Decode_WhitespaceOnly_ReturnsEmptyObject()
    {
        var result = Toon.Decode("   ");

        Assert.Equal(JsonValueKind.Object, result.ValueKind);
        Assert.Equal(0, result.EnumerateObject().Count());
    }

    [Fact]
    public void RoundTrip_EmptyObject_ReturnsEmptyObject()
    {
        var encoded = Toon.Encode(new { });
        var result = Toon.Decode(encoded);

        Assert.Equal(JsonValueKind.Object, result.ValueKind);
        Assert.Equal(0, result.EnumerateObject().Count());
    }

    // Findings 2/13: "[]" (root) and "key: []" (object field) decode as an
    // empty array (spec §4, §9.1), not the literal string "[]".
    [Fact]
    public void Decode_EmptyArrayShorthand_ObjectField_ReturnsEmptyArray()
    {
        var result = Toon.Decode("items: []");

        var items = result.GetProperty("items");
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.Equal(0, items.GetArrayLength());
    }

    [Fact]
    public void Decode_EmptyArrayShorthand_Root_ReturnsEmptyArray()
    {
        var result = Toon.Decode("[]");

        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal(0, result.GetArrayLength());
    }

    // Finding 4: an empty token between delimiters decodes to "" (spec
    // §9.1), not null.
    [Fact]
    public void Decode_EmptyInlineToken_ReturnsEmptyString()
    {
        var result = Toon.Decode("items[3]: a,,c");

        var items = result.GetProperty("items");
        Assert.Equal(JsonValueKind.String, items[1].ValueKind);
        Assert.Equal("", items[1].GetString());
    }

    // Finding 5: tokens with a leading zero decode as strings, not numbers
    // (spec §4).
    [Theory]
    [InlineData("code: 05", "05")]
    [InlineData("code: 0001", "0001")]
    [InlineData("code: -05", "-05")]
    public void Decode_LeadingZeroNumericToken_ReturnsString(string toon, string expected)
    {
        var result = Toon.Decode(toon);

        Assert.Equal(JsonValueKind.String, result.GetProperty("code").ValueKind);
        Assert.Equal(expected, result.GetProperty("code").GetString());
    }

    // Spec §4 carves these shapes out explicitly as still-valid numbers;
    // guards against the fix above over-correcting.
    [Fact]
    public void Decode_ZeroPointFive_RemainsNumber()
    {
        var result = Toon.Decode("value: 0.5");

        Assert.Equal(JsonValueKind.Number, result.GetProperty("value").ValueKind);
        Assert.Equal(0.5, result.GetProperty("value").GetDouble());
    }

    [Fact]
    public void Decode_ZeroExponentOne_RemainsNumber()
    {
        var result = Toon.Decode("value: 0e1");

        Assert.Equal(JsonValueKind.Number, result.GetProperty("value").ValueKind);
    }

    [Fact]
    public void Decode_NegativeZeroPointFive_RemainsNumber()
    {
        var result = Toon.Decode("value: -0.5");

        Assert.Equal(JsonValueKind.Number, result.GetProperty("value").ValueKind);
        Assert.Equal(-0.5, result.GetProperty("value").GetDouble());
    }

    // Findings 6/7: a malformed array-length header ("[03]", "[-1]") MUST be
    // rejected in strict mode (spec §6, §14.2), not silently accepted or
    // degraded to an empty array.
    [Fact]
    public void Decode_LeadingZeroArrayLength_StrictMode_Throws()
    {
        var options = new DecodeOptions { Strict = true };

        Assert.Throws<InvalidOperationException>(() => Toon.Decode("items[03]: 1,2,3", options));
    }

    [Fact]
    public void Decode_NegativeArrayLength_StrictMode_Throws()
    {
        var options = new DecodeOptions { Strict = true };

        Assert.Throws<InvalidOperationException>(() => Toon.Decode("items[-1]:", options));
    }

    // Finding 8: duplicate sibling object keys MUST error in strict mode
    // (spec §8, §14.4).
    [Fact]
    public void Decode_DuplicateKeys_StrictMode_Throws()
    {
        var options = new DecodeOptions { Strict = true };

        Assert.Throws<InvalidOperationException>(() => Toon.Decode("name: Alice\nname: Bob", options));
    }

    // Non-strict last-write-wins is already correct today; kept alongside
    // the strict-mode test above to pin the intended strict/non-strict
    // boundary so a future fix doesn't remove non-strict tolerance too.
    [Fact]
    public void Decode_DuplicateKeys_NonStrictMode_LastWriteWins()
    {
        var options = new DecodeOptions { Strict = false };

        var result = Toon.Decode("name: Alice\nname: Bob", options);

        Assert.Equal("Bob", result.GetProperty("name").GetString());
    }

    // Finding 9: NaN/+Infinity/-Infinity MUST encode as the literal `null`
    // (spec §3), not throw.
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Encode_NonFiniteDouble_EncodesAsNull(double value)
    {
        var result = Toon.Encode(new { value });

        Assert.Equal("value: null", result);
    }

    // Finding 11 (fix-order item 6): encoding a non-uniform array whose
    // first item is itself an array-of-arrays currently drops that item
    // silently — EncodeListItemValue has no fallback for a JsonArray that
    // isn't purely primitive (spec §9.2/§9.4: nested array as list item is
    // "- [M<delim?>]:" then items at depth+2).
    [Fact]
    public void RoundTrip_NonUniformArrayWithNestedArrayOfArraysAsListItem_PreservesAllItems()
    {
        object input = new object[]
        {
            new object[] { new object[] { 1, 2 }, new object[] { 3, 4 } },
            new object[] { 5, 6 }
        };

        var encoded = Toon.Encode(input);
        var result = Toon.Decode(encoded);

        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal(2, result.GetArrayLength());

        var first = result[0];
        Assert.Equal(JsonValueKind.Array, first.ValueKind);
        Assert.Equal(2, first.GetArrayLength());
        Assert.Equal(1, first[0][0].GetInt32());
        Assert.Equal(2, first[0][1].GetInt32());
        Assert.Equal(3, first[1][0].GetInt32());
        Assert.Equal(4, first[1][1].GetInt32());

        var second = result[1];
        Assert.Equal(JsonValueKind.Array, second.ValueKind);
        Assert.Equal(5, second[0].GetInt32());
        Assert.Equal(6, second[1].GetInt32());
    }

    // Same gap, tabular-eligible-array-of-objects sub-case: a uniform array
    // of objects nested as one item of a non-uniform array should still get
    // the compact tabular form, not just avoid being dropped.
    [Fact]
    public void RoundTrip_NonUniformArrayWithTabularArrayOfObjectsAsListItem_PreservesAllItems()
    {
        object input = new object[]
        {
            new object[]
            {
                new { id = 1, name = "Alice" },
                new { id = 2, name = "Bob" }
            },
            "marker"
        };

        var encoded = Toon.Encode(input);
        var result = Toon.Decode(encoded);

        Assert.Equal(2, result.GetArrayLength());

        var users = result[0];
        Assert.Equal(JsonValueKind.Array, users.ValueKind);
        Assert.Equal(2, users.GetArrayLength());
        Assert.Equal(1, users[0].GetProperty("id").GetInt32());
        Assert.Equal("Alice", users[0].GetProperty("name").GetString());
        Assert.Equal(2, users[1].GetProperty("id").GetInt32());
        Assert.Equal("Bob", users[1].GetProperty("name").GetString());

        Assert.Equal("marker", result[1].GetString());
    }

    // Finding 10 (fix-order item 7): a nested array header with no explicit
    // delimiter suffix must default to comma, never inherit the parent
    // array's delimiter (spec §6: "absence means comma, never inherited").
    [Fact]
    public void Decode_NestedArrayWithoutDelimiterSuffix_DefaultsToCommaNotParentDelimiter()
    {
        var toon = "matrix[2|]:\n  - [2]: 1,2\n  - [2]: 3,4";
        var options = new DecodeOptions { Strict = false };

        var result = Toon.Decode(toon, options);

        var matrix = result.GetProperty("matrix");
        Assert.Equal(2, matrix.GetArrayLength());
        var first = matrix[0];
        Assert.Equal(2, first.GetArrayLength());
        Assert.Equal(1, first[0].GetInt32());
        Assert.Equal(2, first[1].GetInt32());
    }

    // Finding 12 (fix-order item 7), encoder side: when a tabular array is
    // the first field of a list-item object, its header goes on the hyphen
    // line but its rows belong at hyphenDepth+2, and sibling fields at
    // hyphenDepth+1 (spec §10) — not both at hyphenDepth+1.
    [Fact]
    public void Encode_TabularArrayAsFirstFieldOfListItem_PlacesRowsAtDepthPlusTwo()
    {
        object input = new object[]
        {
            new
            {
                users = new object[]
                {
                    new { id = 1, name = "Alice" },
                    new { id = 2, name = "Bob" }
                },
                active = true
            }
        };

        var result = Toon.Encode(input);

        var expected = string.Join("\n",
            "[1]:",
            "  - users[2]{id,name}:",
            "      1,Alice",
            "      2,Bob",
            "    active: true");
        Assert.Equal(expected, result);
    }

    // Same finding, decoder side: rows at hyphenDepth+2 must be read back
    // correctly, with the sibling field at hyphenDepth+1 not mistaken for a
    // row nor colliding with the row range.
    [Fact]
    public void Decode_TabularArrayAsFirstFieldOfListItem_ReadsRowsAtDepthPlusTwo()
    {
        var toon = "[1]:\n  - users[2]{id,name}:\n      1,Alice\n      2,Bob\n    active: true";

        var result = Toon.Decode(toon);

        var first = result[0];
        var users = first.GetProperty("users");
        Assert.Equal(2, users.GetArrayLength());
        Assert.Equal(1, users[0].GetProperty("id").GetInt32());
        Assert.Equal("Alice", users[0].GetProperty("name").GetString());
        Assert.Equal(2, users[1].GetProperty("id").GetInt32());
        Assert.Equal("Bob", users[1].GetProperty("name").GetString());
        Assert.True(first.GetProperty("active").GetBoolean());
    }

    // Fix-order item 8: other C0 controls (U+0000-001F besides \n/\r/\t)
    // must be emitted as \uXXXX on encode (spec §7.1, also a §15 security
    // concern — raw control bytes in output otherwise).
    [Fact]
    public void Encode_ControlCharacterOutsideNamedEscapes_EmitsUnicodeEscape()
    {
        var result = Toon.Encode(new { value = "a" + '' + "b" });

        Assert.Equal("value: \"a\\u0001b\"", result);
    }

    // Decoder counterpart: \uXXXX must be recognized and decoded back.
    [Fact]
    public void Decode_UnicodeEscape_DecodesToCharacter()
    {
        var result = Toon.Decode("value: \"a\\u0041b\"");

        Assert.Equal("aAb", result.GetProperty("value").GetString());
    }

    // Case-insensitivity (spec §7.1) applies to the hex digits, not the
    // escape marker: the grammar (%x75 4HEXDIG) only defines lowercase "u",
    // so "\U0041" is correctly rejected as an unrecognized escape, not
    // treated as an alternate spelling of "A".
    [Fact]
    public void Decode_UnicodeEscape_HexDigitsAreCaseInsensitive()
    {
        var lower = Toon.Decode("value: \"a\\u00e9b\"");
        var upper = Toon.Decode("value: \"a\\u00E9b\"");

        Assert.Equal(lower.GetProperty("value").GetString(), upper.GetProperty("value").GetString());
        Assert.Equal("aéb", lower.GetProperty("value").GetString());
    }

    [Fact]
    public void RoundTrip_StringWithControlCharacter_PreservesExactValue()
    {
        var original = "a" + '' + "b" + '' + "c";

        var encoded = Toon.Encode(new { value = original });
        var result = Toon.Decode(encoded);

        Assert.Equal(original, result.GetProperty("value").GetString());
    }

    // Spec §7.1: decoder MUST reject \u with fewer than 4 hex digits.
    [Fact]
    public void Decode_UnicodeEscapeWithTooFewHexDigits_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Toon.Decode("value: \"a\\u01b\""));
    }

    // Spec §7.1: decoder MUST reject surrogate code points from \uXXXX.
    [Fact]
    public void Decode_UnicodeEscapeWithSurrogateCodePoint_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Toon.Decode("value: \"a\\ud800b\""));
    }

    // Spec §7.1: decoder MUST reject any escape sequence not in the table,
    // not silently pass it through as literal text.
    [Fact]
    public void Decode_UnrecognizedEscapeSequence_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Toon.Decode("value: \"a\\xb\""));
    }

    // Finding 14 (fix-order item 9): unquoted key encoding must use §7.3's
    // stricter identifier pattern (^[A-Za-z_][A-Za-z0-9_.]*$), not the
    // value-quoting rule set (§7.2) it currently reuses.
    [Fact]
    public void Encode_KeyStartingWithDigit_IsQuoted()
    {
        var input = new Dictionary<string, object> { ["123abc"] = 1 };

        var result = Toon.Encode(input);

        Assert.Equal("\"123abc\": 1", result);
    }

    [Fact]
    public void Encode_KeyWithInternalSpace_IsQuoted()
    {
        var input = new Dictionary<string, object> { ["first name"] = "Alice" };

        var result = Toon.Encode(input);

        Assert.Equal("\"first name\": Alice", result);
    }

    // Regression guard: legitimate identifier-shaped keys (including a
    // dotted key, which is a literal key per key-folding "off") must stay
    // unquoted — the §7.3 rule shouldn't over-quote.
    [Fact]
    public void Encode_ValidIdentifierKeys_RemainUnquoted()
    {
        var input = new Dictionary<string, object> { ["user_id"] = 1, ["a.b.c"] = 2, ["_private"] = 3 };

        var result = Toon.Encode(input);

        Assert.Contains("user_id: 1", result);
        Assert.Contains("a.b.c: 2", result);
        Assert.Contains("_private: 3", result);
    }

    // Finding 15 (fix-order item 9): two missing §7.2 MUST-quote
    // conditions. The whitespace case is a real self-round-trip bug: this
    // library's own decoder trims unquoted values, so leaving them
    // unquoted silently strips the padding back out on read.
    [Fact]
    public void Encode_ValueWithLeadingTrailingWhitespace_IsQuoted()
    {
        var result = Toon.Encode(new { value = "  padded  " });

        Assert.Equal("value: \"  padded  \"", result);
    }

    [Fact]
    public void RoundTrip_ValueWithLeadingTrailingWhitespace_PreservesExactValue()
    {
        var original = "  padded  ";

        var encoded = Toon.Encode(new { value = original });
        var result = Toon.Decode(encoded);

        Assert.Equal(original, result.GetProperty("value").GetString());
    }

    [Fact]
    public void Encode_ValueStartingWithHyphen_IsQuoted()
    {
        var result = Toon.Encode(new { value = "-foo" });

        Assert.Equal("value: \"-foo\"", result);
    }

    [Fact]
    public void Encode_ValueEqualToHyphen_IsQuoted()
    {
        var result = Toon.Encode(new { value = "-" });

        Assert.Equal("value: \"-\"", result);
    }

    // Finding 18 (fix-order item 10, minimal fix): value quoting must be
    // delimiter-aware — a string containing a delimiter OTHER than the one
    // actually configured doesn't need quoting on that basis alone (spec
    // §11.1). LiteralUtils.FormatPrimitive already receives `delimiter` but
    // never forwarded it to EscapeString, which unconditionally quoted for
    // comma, pipe, AND tab regardless of what was configured.
    [Fact]
    public void Encode_ValueContainingNonActiveDelimiter_RemainsUnquoted()
    {
        // Default delimiter is comma; the value contains a pipe.
        var result = Toon.Encode(new { value = "a|b" });

        Assert.Equal("value: a|b", result);
    }

    [Fact]
    public void Encode_ValueContainingCommaWithPipeDelimiter_RemainsUnquoted()
    {
        var options = new EncodeOptions { Delimiter = '|' };

        var result = Toon.Encode(new { value = "a,b" }, options);

        Assert.Equal("value: a,b", result);
    }

    // Regression guards: a value containing the delimiter that IS active
    // must still be quoted.
    [Fact]
    public void Encode_ValueContainingActiveDelimiter_Comma_IsQuoted()
    {
        var result = Toon.Encode(new { value = "a,b" });

        Assert.Equal("value: \"a,b\"", result);
    }

    [Fact]
    public void Encode_ValueContainingActiveDelimiter_Pipe_IsQuoted()
    {
        var options = new EncodeOptions { Delimiter = '|' };

        var result = Toon.Encode(new { value = "a|b" }, options);

        Assert.Equal("value: \"a|b\"", result);
    }

    // Tab is always quoted regardless of the active delimiter, since it's
    // separately a control character (§7.2) — not just a delimiter guard.
    [Fact]
    public void Encode_ValueContainingTab_IsQuotedRegardlessOfDelimiter()
    {
        var options = new EncodeOptions { Delimiter = '|' };

        var result = Toon.Encode(new { value = "a\tb" }, options);

        Assert.Equal("value: \"a\\tb\"", result);
    }

    // Finding 16 (fix-order item 11): number canonical form must use the
    // shortest round-trippable representation, not "G17" (spec §2).
    [Fact]
    public void Encode_DoubleValue_UsesShortestRoundTrippableForm()
    {
        var result = Toon.Encode(new { value = 0.1 });

        Assert.Equal("value: 0.1", result);
    }

    [Fact]
    public void RoundTrip_TrickyDoubleValue_PreservesExactValue()
    {
        double original = 0.1 + 0.2; // 0.30000000000000004 in IEEE754

        var encoded = Toon.Encode(new { value = original });
        var result = Toon.Decode(encoded);

        Assert.Equal(original, result.GetProperty("value").GetDouble());
    }

    // Spec §2: canonical form has NO exponent for 0 or 1e-6 <= |n| < 1e21 —
    // this is a MUST, not just a verbosity preference. .NET's shortest
    // round-trip format switches to scientific notation well before 1e21
    // (around 1e17), so values in that gap need explicit expansion back to
    // fixed-point.
    [Fact]
    public void Encode_LargeDoubleWithinNoExponentRange_DoesNotUseExponentNotation()
    {
        double value = 1e19;

        var result = Toon.Encode(new { value });

        Assert.Equal("value: 10000000000000000000", result);
    }

    [Fact]
    public void RoundTrip_LargeDoubleWithinNoExponentRange_PreservesExactValue()
    {
        double original = 1e19;

        var encoded = Toon.Encode(new { value = original });
        var result = Toon.Decode(encoded);

        Assert.Equal(original, result.GetProperty("value").GetDouble());
    }

    [Fact]
    public void Encode_SmallDoubleWithinNoExponentRange_DoesNotUseExponentNotation()
    {
        double value = 1.5e-6;

        var result = Toon.Encode(new { value });

        Assert.Equal("value: 0.0000015", result);
    }

    // Outside the no-exponent range, spec's own example ("1e-7", "1e+21")
    // uses a lowercase exponent marker.
    [Fact]
    public void Encode_ValueOutsideNoExponentRange_UsesLowercaseExponentMarker()
    {
        double value = 1e-7;

        var result = Toon.Encode(new { value });

        Assert.DoesNotContain("E", result);
        Assert.Contains("e", result);
    }

    // Finding 17 (fix-order item 11): the "numeric-like" quoting check must
    // use the spec's exact regex, not double.TryParse, which both over-
    // and under-quotes relative to it. The v3.3.2 regex is
    // ^-?\d+(?:\.\d+)?(?:e[+-]?\d+)?$ — no leading "+" — so genuine
    // v3.3.2-compliant output does NOT quote "+5", even though this is a
    // known spec-acknowledged interop gap (an unquoted "+5" decodes back
    // as the number 5 on any decoder). v4.0.0 widens the sign class to
    // [+-] to close that gap (TOON_V4.md phase 4 step 15), and — per
    // TOON_V4.md phase 5 — EncodeOptions.SpecVersion now DEFAULTS to V4,
    // so this v3.3.2-specific behavior needs explicit SpecVersion = V3 to
    // observe; see Encode_StringLeadingPlusInteger_V4SpecVersion_IsQuoted
    // in ToonV4ComplianceTests.cs for the (now-default) v4 behavior.
    [Fact]
    public void Encode_LeadingPlusNumericLikeString_RemainsUnquotedUnderV3()
    {
        var result = Toon.Encode(new { value = "+5" }, new EncodeOptions { SpecVersion = ToonSpecVersion.V3 });

        Assert.Equal("value: +5", result);
    }

    [Fact]
    public void Encode_ThousandsSeparatorString_NotQuotedForNumericLikeReason()
    {
        // With a non-comma delimiter, "1,000" isn't quoted for containing
        // the active delimiter — isolating whether it's still incorrectly
        // quoted for "looking like a number" (double.TryParse with default
        // NumberStyles accepts thousands separators; the exact regex does
        // not).
        var options = new EncodeOptions { Delimiter = '|' };

        var result = Toon.Encode(new { value = "1,000" }, options);

        Assert.Equal("value: 1,000", result);
    }

    [Fact]
    public void Encode_AllDigitStringExceedingDoubleRange_IsQuoted()
    {
        var hugeDigitString = new string('9', 400);

        var result = Toon.Encode(new { value = hugeDigitString });

        Assert.Equal($"value: \"{hugeDigitString}\"", result);
    }

    // Finding 19 (fix-order item 12): strict mode must error on a blank
    // line ANYWHERE inside an array's row/item range (spec §12), not just
    // between the header and the first row — ValidateNoBlankLinesInRange
    // was only ever called checking that first gap.
    [Fact]
    public void Decode_BlankLineBetweenTabularRows_StrictMode_Throws()
    {
        var toon = "users[3]{id,name}:\n  1,Alice\n  2,Bob\n\n  3,Charlie";

        Assert.Throws<InvalidOperationException>(() => Toon.Decode(toon));
    }

    [Fact]
    public void Decode_BlankLineBetweenListItems_StrictMode_Throws()
    {
        var toon = "items[3]:\n  - a\n  - b\n\n  - c";

        Assert.Throws<InvalidOperationException>(() => Toon.Decode(toon));
    }

    // Non-strict mode MAY ignore blank lines inside arrays without
    // counting them as a row/item (spec §12) — already correct today,
    // since blank lines aren't part of the main line stream at all; kept
    // as a regression guard alongside the strict-mode tests above.
    [Fact]
    public void Decode_BlankLineBetweenTabularRows_NonStrictMode_Ignored()
    {
        var options = new DecodeOptions { Strict = false };
        var toon = "users[3]{id,name}:\n  1,Alice\n  2,Bob\n\n  3,Charlie";

        var result = Toon.Decode(toon, options);

        var users = result.GetProperty("users");
        Assert.Equal(3, users.GetArrayLength());
        Assert.Equal("Charlie", users[2].GetProperty("name").GetString());
    }

    // Finding 20 (fix-order item 12): non-strict mode MAY fall through to
    // key-value parsing instead of erroring on a malformed bracket segment
    // (spec §14.2) — spec-legal either way, but the more lenient option was
    // never implemented; the parser threw unconditionally.
    [Fact]
    public void Decode_MalformedArrayHeader_NonStrictMode_FallsThroughToKeyValueParsing()
    {
        var options = new DecodeOptions { Strict = false };

        var result = Toon.Decode("items[abc]: value", options);

        Assert.Equal("value", result.GetProperty("items[abc]").GetString());
    }

    // Strict mode must still error — regression guard alongside the
    // existing ParseArrayHeaderLine_InvalidLength_Throws parser-level test.
    [Fact]
    public void Decode_MalformedArrayHeader_StrictMode_StillThrows()
    {
        Assert.Throws<InvalidOperationException>(() => Toon.Decode("items[abc]: value"));
    }

    // Finding 21 (fix-order item 13): CRLF line endings should be
    // deliberately normalized at the scanner boundary (spec §1.2), not
    // just incidentally tolerated by scattered downstream .Trim() calls.
    // End-to-end confirmation across object fields, nested objects, and a
    // tabular array — scanner-level coverage lives in ToonScannerTests.cs.
    [Fact]
    public void Decode_CrlfLineEndings_DecodesCorrectly()
    {
        var toon = "name: Alice\r\nage: 30\r\nusers[2]{id,name}:\r\n  1,Bob\r\n  2,Carol";

        var result = Toon.Decode(toon);

        Assert.Equal("Alice", result.GetProperty("name").GetString());
        Assert.Equal(30, result.GetProperty("age").GetInt32());
        var users = result.GetProperty("users");
        Assert.Equal(2, users.GetArrayLength());
        Assert.Equal("Bob", users[0].GetProperty("name").GetString());
        Assert.Equal("Carol", users[1].GetProperty("name").GetString());
    }
}
