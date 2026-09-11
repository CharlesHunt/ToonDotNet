namespace ToonFormat.Tests;

// Tracks progress on TOON_V4.md's implementation order (the v4.0.0+ gap
// list). Phase 0 (pinning the spec baseline) is covered by
// ToonBasicTests.Constants_SpecVersion_MatchesDocumentedBaseline. This
// file starts at phase 1.
public class ToonV4ComplianceTests
{
    // Phase 1, step 2: EncodeOptions.SpecVersion exists but is
    // deliberately inert until phase 5 (v4-only encoder features land and
    // the default flips). The "IsCurrentlyInert" test is EXPECTED to
    // start failing once that happens — at that point, replace it with
    // tests for the actual v4-only output differences rather than
    // "no difference."
    [Fact]
    public void EncodeOptions_SpecVersion_DefaultsToV3()
    {
        var options = new EncodeOptions();

        Assert.Equal(ToonSpecVersion.V3, options.SpecVersion);
    }

    [Fact]
    public void EncodeOptions_SpecVersion_IsCurrentlyInert()
    {
        var data = new { users = new[] { new { id = 1, name = "Alice" } } };

        var v3Output = Toon.Encode(data, new EncodeOptions { SpecVersion = ToonSpecVersion.V3 });
        var v4Output = Toon.Encode(data, new EncodeOptions { SpecVersion = ToonSpecVersion.V4 });

        Assert.Equal(v3Output, v4Output);
    }

    // Phase 1, step 3: DecodeOptions.LegacyCompatibility exists but is
    // deliberately inert until phase 4 (the three semantic-conflict rules
    // are implemented). The "IsCurrentlyInert" test is expected to start
    // failing once that happens.
    [Fact]
    public void DecodeOptions_LegacyCompatibility_DefaultsToFalse()
    {
        var options = new DecodeOptions();

        Assert.False(options.LegacyCompatibility);
    }

    [Fact]
    public void DecodeOptions_LegacyCompatibility_IsCurrentlyInert()
    {
        var toon = "users[1]{id,name}:\n  1,Alice";

        var defaultResult = Toon.Decode(toon, new DecodeOptions { LegacyCompatibility = false });
        var legacyResult = Toon.Decode(toon, new DecodeOptions { LegacyCompatibility = true });

        Assert.Equal(defaultResult.GetRawText(), legacyResult.GetRawText());
    }

    // Phase 2, step 10: v4.0.0 classifies length-less bracket segments
    // (e.g. "items[]:") as malformed headers. Already correctly rejected
    // in strict mode (the default) as a side effect of the v3 work
    // (TOON_V3.md findings 6/7/20) — ParseBracketSegment already throws
    // for empty bracket content, and non-strict mode already falls
    // through to key-value parsing instead. This test makes that
    // intentional rather than incidental, and closes this v4 gap-list row
    // without any new code.
    [Fact]
    public void Decode_LengthLessBracketSegment_StrictMode_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Toon.Decode("items[]: value"));
    }

    [Fact]
    public void Decode_LengthLessBracketSegment_NonStrictMode_FallsThroughToKeyValueParsing()
    {
        var options = new DecodeOptions { Strict = false };

        var result = Toon.Decode("items[]: value", options);

        Assert.Equal("value", result.GetProperty("items[]").GetString());
    }

    // Phase 2, step 4: v4.0.0 §9.2 requires decoders to accept the bare
    // list item "- []" as an empty inner array (equivalent to "- [0]:").
    // Already correctly decoded as a side effect of the v3 "[]" empty-array
    // literal fix (TOON_V3.md) — LiteralUtils.ParsePrimitiveToken("[]")
    // already returns an empty JSON array, and DecodeListArray's primitive
    // branch reaches it whenever a list item has no colon. This test makes
    // that intentional rather than incidental, and closes this v4 gap-list
    // row without any new code.
    [Fact]
    public void Decode_BareEmptyBracketListItem_DecodesAsEmptyInnerArray()
    {
        string toon = "matrix[2]:\n  - []\n  - [2]: 1,2";

        var result = Toon.Decode(toon);

        var matrix = result.GetProperty("matrix").EnumerateArray().ToArray();
        Assert.Equal(2, matrix.Length);
        Assert.Equal(0, matrix[0].GetArrayLength());
        Assert.Equal(new[] { 1, 2 }, matrix[1].EnumerateArray().Select(e => e.GetInt32()));
    }

    [Fact]
    public void Decode_BareEmptyBracketListItem_EquivalentToExplicitZeroLengthForm()
    {
        string bareForm = "matrix[1]:\n  - []";
        string explicitForm = "matrix[1]:\n  - [0]:";

        var bareResult = Toon.Decode(bareForm);
        var explicitResult = Toon.Decode(explicitForm);

        Assert.Equal(explicitResult.GetRawText(), bareResult.GetRawText());
    }

    // Phase 2, step 5: v4.0.0 §5.1 makes full-line "#" comments normative.
    // A comment line is a line whose first character after zero or more
    // leading spaces is '#' (tabs cannot precede it). Decoders MUST strip
    // comment lines in a lexical pre-pass, in strict and non-strict mode
    // alike. Comments are full-line only — a "#" anywhere else on a line
    // (e.g. after a key, or inside a quoted value) is ordinary content.
    [Fact]
    public void Decode_FullLineComment_IsStrippedFromDocument()
    {
        string toon = "# this is a comment\nname: Alice\n# another comment\nage: 30";

        var result = Toon.Decode(toon);

        Assert.Equal("Alice", result.GetProperty("name").GetString());
        Assert.Equal(30, result.GetProperty("age").GetInt32());
    }

    [Fact]
    public void Decode_IndentedFullLineComment_IsStrippedFromDocument()
    {
        string toon = "users[1]{id,name}:\n  # a comment indented like a row\n  1,Alice";

        var result = Toon.Decode(toon);

        var users = result.GetProperty("users").EnumerateArray().ToArray();
        Assert.Single(users);
        Assert.Equal("Alice", users[0].GetProperty("name").GetString());
    }

    [Fact]
    public void Decode_CommentLine_StrippedInNonStrictModeToo()
    {
        string toon = "# comment\nname: Alice";

        var result = Toon.Decode(toon, new DecodeOptions { Strict = false });

        Assert.Equal("Alice", result.GetProperty("name").GetString());
    }

    [Fact]
    public void Decode_HashNotAtLineStart_IsOrdinaryContent()
    {
        // "#" only introduces a comment when it is the first character on
        // the line (after leading spaces); elsewhere it is ordinary
        // content and must not be stripped.
        string toon = "note: see #42 for details";

        var result = Toon.Decode(toon);

        Assert.Equal("see #42 for details", result.GetProperty("note").GetString());
    }

    [Fact]
    public void Decode_QuotedHashLeadingValue_IsNotTreatedAsComment()
    {
        string toon = "note: \"#42\"";

        var result = Toon.Decode(toon);

        Assert.Equal("#42", result.GetProperty("note").GetString());
    }

    [Fact]
    public void Encode_StringStartingWithHash_IsQuoted()
    {
        var data = new { note = "#42" };

        string result = Toon.Encode(data);

        Assert.Contains("note: \"#42\"", result);
    }

    [Fact]
    public void RoundTrip_StringStartingWithHash_PreservesValue()
    {
        var data = new { note = "#42" };

        var roundTripped = Toon.RoundTrip(data);

        Assert.Equal("#42", roundTripped.GetProperty("note").GetString());
    }

    // Phase 2, step 6: v4.0.0 RFC #46 adds nested field groups in
    // tabular headers, e.g. "orders[2]{id,customer{name,country},total}:".
    // A column is nested-uniform when every value is a non-empty object,
    // all with the same key set, and every sub-column is itself
    // uniform-primitive or nested-uniform, recursively.
    [Fact]
    public void Encode_ArrayOfObjectsWithNestedUniformColumn_UsesNestedFieldGroup()
    {
        var data = new[]
        {
            new { id = 1, customer = new { name = "Ada", country = "DK" }, total = 99 },
            new { id = 2, customer = new { name = "Bob", country = "UK" }, total = 149 }
        };

        string result = Toon.Encode(data);

        Assert.Contains("[2]{id,customer{name,country},total}:", result);
        Assert.Contains("1,Ada,DK,99", result);
        Assert.Contains("2,Bob,UK,149", result);
    }

    [Fact]
    public void Decode_NestedFieldGroupHeader_DecodesIntoNestedObjects()
    {
        string toon = "orders[2]{id,customer{name,country},total}:\n  1,Ada,DK,99\n  2,Bob,UK,149";

        var result = Toon.Decode(toon);

        var orders = result.GetProperty("orders").EnumerateArray().ToArray();
        Assert.Equal(2, orders.Length);
        Assert.Equal(1, orders[0].GetProperty("id").GetInt32());
        Assert.Equal("Ada", orders[0].GetProperty("customer").GetProperty("name").GetString());
        Assert.Equal("DK", orders[0].GetProperty("customer").GetProperty("country").GetString());
        Assert.Equal(99, orders[0].GetProperty("total").GetInt32());
        Assert.Equal("Bob", orders[1].GetProperty("customer").GetProperty("name").GetString());
    }

    [Fact]
    public void RoundTrip_NestedFieldGroupTabular_PreservesStructure()
    {
        var data = new[]
        {
            new { id = 1, customer = new { name = "Ada", country = "DK" }, total = 99 },
            new { id = 2, customer = new { name = "Bob", country = "UK" }, total = 149 }
        };

        var roundTripped = Toon.RoundTrip(data);

        Assert.Equal(
            Toon.Decode(Toon.Encode(data)).GetRawText(),
            roundTripped.GetRawText());
        var firstOrder = roundTripped.EnumerateArray().First();
        Assert.Equal("Ada", firstOrder.GetProperty("customer").GetProperty("name").GetString());
    }

    [Fact]
    public void Decode_DeeplyNestedFieldGroups_DecodesRecursively()
    {
        // Nesting depth is unbounded per spec §9.3.
        string toon = "items[1]{id,a{b{c}}}:\n  1,42";

        var result = Toon.Decode(toon);

        var items = result.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(42, items[0].GetProperty("a").GetProperty("b").GetProperty("c").GetInt32());
    }

    [Fact]
    public void Decode_NestedFieldGroupRow_StrictMode_WrongCellCount_Throws()
    {
        // Header declares 3 leaf fields (id, customer.name, customer.country)
        // but this row only supplies 2 cells.
        string toon = "orders[1]{id,customer{name,country}}:\n  1,Ada";

        Assert.Throws<InvalidOperationException>(() => Toon.Decode(toon));
    }

    [Fact]
    public void Decode_NestedFieldGroupRow_NonStrictMode_WrongCellCount_DoesNotThrow()
    {
        string toon = "orders[1]{id,customer{name,country}}:\n  1,Ada";

        var options = new DecodeOptions { Strict = false };

        var result = Toon.Decode(toon, options);

        Assert.NotEqual(default, result.ValueKind);
    }

    [Fact]
    public void Encode_ArrayOfObjectsWithNonUniformNestedColumn_FallsBackToListForm()
    {
        // "extra" object columns don't share the same key set across
        // rows, so the column is neither uniform-primitive nor
        // nested-uniform: the whole array must fall back to list form
        // (spec §9.3/§9.4), not a partially-tabular hybrid.
        var data = new object[]
        {
            new { id = 1, extra = new { a = 1 } },
            new { id = 2, extra = new { b = 2 } }
        };

        string result = Toon.Encode(data);

        Assert.DoesNotContain("{id,extra", result);
        Assert.Contains("- id: 1", result);
    }

    // Phase 2, step 7: v4.0.0 RFC #57 adds keyed tabular form for
    // objects (spec §9.5): an object with at least two entries whose
    // values are uniform non-empty objects collapses into
    // "key[N:delim?]{fields}:" followed by "entrykey: cells" rows. Note
    // the literal colon inside the bracket segment (keyed-seg grammar,
    // §6) — distinct from a plain array/tabular header.
    [Fact]
    public void Encode_ObjectOfUniformObjects_UsesKeyedTabularForm()
    {
        var data = new
        {
            users = new Dictionary<string, object>
            {
                ["alice"] = new { age = 30, city = "Berlin" },
                ["bob"] = new { age = 25, city = "Oslo" }
            }
        };

        string result = Toon.Encode(data);

        Assert.Contains("users[2:]{age,city}:", result);
        Assert.Contains("alice: 30,Berlin", result);
        Assert.Contains("bob: 25,Oslo", result);
    }

    [Fact]
    public void Decode_KeyedTabularHeader_DecodesIntoObjectWithEntryKeys()
    {
        string toon = "users[2:]{age,city}:\n  alice: 30,Berlin\n  bob: 25,Oslo";

        var result = Toon.Decode(toon);

        var users = result.GetProperty("users");
        Assert.Equal(30, users.GetProperty("alice").GetProperty("age").GetInt32());
        Assert.Equal("Berlin", users.GetProperty("alice").GetProperty("city").GetString());
        Assert.Equal("Oslo", users.GetProperty("bob").GetProperty("city").GetString());
    }

    [Fact]
    public void Decode_RootKeyedTabularHeader_DecodesIntoObject()
    {
        string toon = "[2:]{age,city}:\n  alice: 30,Berlin\n  bob: 25,Oslo";

        var result = Toon.Decode(toon);

        Assert.Equal(30, result.GetProperty("alice").GetProperty("age").GetInt32());
        Assert.Equal("Oslo", result.GetProperty("bob").GetProperty("city").GetString());
    }

    [Fact]
    public void RoundTrip_ObjectOfUniformObjects_PreservesStructure()
    {
        var data = new
        {
            users = new Dictionary<string, object>
            {
                ["alice"] = new { age = 30, city = "Berlin" },
                ["bob"] = new { age = 25, city = "Oslo" }
            }
        };

        var roundTripped = Toon.RoundTrip(data);

        var users = roundTripped.GetProperty("users");
        Assert.Equal(30, users.GetProperty("alice").GetProperty("age").GetInt32());
        Assert.Equal("Oslo", users.GetProperty("bob").GetProperty("city").GetString());
    }

    [Fact]
    public void Encode_SingleEntryObjectOfObjects_DoesNotUseKeyedTabularForm()
    {
        // Spec §9.5 requires at least two entries; encoders never emit
        // keyed headers for fewer.
        var data = new
        {
            users = new Dictionary<string, object>
            {
                ["alice"] = new { age = 30, city = "Berlin" }
            }
        };

        string result = Toon.Encode(data);

        Assert.DoesNotContain("[1:]", result);
        Assert.Contains("alice:", result);
    }

    [Fact]
    public void Decode_KeyedTabularHeader_ZeroEntries_DecodesToEmptyObject()
    {
        // Spec §9.5: decoders MUST accept any declared entry count N >= 0
        // even though encoders never emit N < 2; "key[0:]{f}:" with no
        // entry rows decodes to {}.
        string toon = "users[0:]{age}:";

        var result = Toon.Decode(toon);

        Assert.Equal(0, result.GetProperty("users").EnumerateObject().Count());
    }

    [Fact]
    public void Decode_KeyedTabularEntryRow_StrictMode_WrongCellCount_Throws()
    {
        string toon = "users[2:]{age,city}:\n  alice: 30,Berlin\n  bob: 25";

        Assert.Throws<InvalidOperationException>(() => Toon.Decode(toon));
    }

    [Fact]
    public void Decode_KeyedTabularHeader_MissingFieldList_StrictMode_Throws()
    {
        // Spec §9.5: the field list is REQUIRED for a keyed-tabular
        // header.
        Assert.Throws<InvalidOperationException>(() => Toon.Decode("users[2:]:\n  alice: 30\n  bob: 25"));
    }

    [Fact]
    public void Decode_KeyedTabularHeader_MissingFieldList_NonStrictMode_DoesNotThrow()
    {
        // Non-strict fall-through for a malformed header is
        // implementation-defined (spec §14.2 "MAY"), not normative —
        // unlike the length-less bracket case (a single, unambiguous
        // colon), a missing-field-list keyed header has two colons on
        // the line, so this only asserts non-strict mode tolerates it
        // rather than pinning an exact fallback shape.
        var options = new DecodeOptions { Strict = false };

        var result = Toon.Decode("users[2:]: value", options);

        Assert.NotEqual(default, result.ValueKind);
    }

    [Fact]
    public void Encode_ArrayOfObjectsIsNeverEncodedAsKeyedTabular()
    {
        // Spec §10: array elements are anonymous and never encode in
        // keyed tabular form, even though their shape (uniform objects)
        // would otherwise qualify — this is genuinely the §9.3 tabular
        // case, not §9.5.
        var data = new[]
        {
            new { age = 30, city = "Berlin" },
            new { age = 25, city = "Oslo" }
        };

        string result = Toon.Encode(data);

        Assert.DoesNotContain(":]", result);
        Assert.Contains("[2]{age,city}:", result);
    }

    // Phase 2, step 8: v4.0.0 exempts tabular key reordering from the
    // §2 round-trip predicate — decoded key order for a tabular row is
    // the header's field order, not necessarily the original object's
    // property order (spec §9.3: "order per object MAY vary"). Already
    // correctly implemented as a side effect of how tabular
    // encoding/decoding was built (step 6's work): tabular-detection
    // only checks that each row has the SAME KEY SET as the header, not
    // the same key ORDER (AllRowsHaveExactKeySet), and decoding always
    // rebuilds each row by walking the header's field list, not the
    // input's property order. There's no Diff/equality-comparison helper
    // in this codebase for a "loosen the comparison" fix to apply to —
    // this test makes the existing tolerance intentional instead.
    [Fact]
    public void Encode_ArrayOfObjectsWithDifferentKeyOrderPerRow_StillUsesTabularForm()
    {
        var doc = System.Text.Json.JsonDocument.Parse(
            "[{\"id\":1,\"name\":\"Alice\"},{\"name\":\"Bob\",\"id\":2}]");

        string result = Toon.Encode(doc.RootElement);

        Assert.Contains("[2]{id,name}:", result);
        Assert.Contains("1,Alice", result);
        Assert.Contains("2,Bob", result);
    }

    [Fact]
    public void RoundTrip_ArrayOfObjectsWithDifferentKeyOrderPerRow_NormalizesToHeaderOrder()
    {
        var doc = System.Text.Json.JsonDocument.Parse(
            "[{\"id\":1,\"name\":\"Alice\"},{\"name\":\"Bob\",\"id\":2}]");

        var roundTripped = Toon.RoundTrip(doc.RootElement);

        Assert.Equal(
            "[{\"id\":1,\"name\":\"Alice\"},{\"id\":2,\"name\":\"Bob\"}]",
            roundTripped.GetRawText());
    }

    // Phase 2, step 9: v4.0.0 makes prototype-key handling normative
    // (§15) — decoders MUST materialize every key, including
    // "__proto__", "constructor", and "prototype", as an ordinary own
    // entry, and decoding MUST NOT mutate prototype chains, class
    // metadata, or any other shared state of the host object model.
    // Already safe by construction for this .NET decoder: decoded
    // objects are built as Dictionary<string, JsonElement> (no prototype
    // chain exists to pollute in .NET the way JS object literals have
    // one), and System.Text.Json gives these names no special treatment
    // either. These tests make that safety intentional rather than
    // incidental, across every key position the header/field grammar
    // supports.
    [Fact]
    public void Decode_DangerousKeyNames_AsObjectFields_MaterializeAsOrdinaryEntries()
    {
        string toon = "__proto__: evil\nconstructor: c\nprototype: p\nnormal: ok";

        var result = Toon.Decode(toon);

        Assert.Equal("evil", result.GetProperty("__proto__").GetString());
        Assert.Equal("c", result.GetProperty("constructor").GetString());
        Assert.Equal("p", result.GetProperty("prototype").GetString());
        Assert.Equal("ok", result.GetProperty("normal").GetString());
    }

    [Fact]
    public void Decode_DangerousKeyNames_AsTabularFieldNames_MaterializeAsOrdinaryEntries()
    {
        string toon = "items[1]{__proto__,constructor}:\n  a,b";

        var result = Toon.Decode(toon);

        var item = result.GetProperty("items").EnumerateArray().First();
        Assert.Equal("a", item.GetProperty("__proto__").GetString());
        Assert.Equal("b", item.GetProperty("constructor").GetString());
    }

    [Fact]
    public void Decode_DangerousKeyNames_AsKeyedTabularEntryKeys_MaterializeAsOrdinaryEntries()
    {
        string toon = "table[2:]{age}:\n  __proto__: 1\n  constructor: 2";

        var result = Toon.Decode(toon);

        var table = result.GetProperty("table");
        Assert.Equal(1, table.GetProperty("__proto__").GetProperty("age").GetInt32());
        Assert.Equal(2, table.GetProperty("constructor").GetProperty("age").GetInt32());
    }

    [Fact]
    public void RoundTrip_DangerousKeyNames_PreservesValuesExactly()
    {
        var doc = System.Text.Json.JsonDocument.Parse(
            "{\"__proto__\":\"evil\",\"constructor\":\"c\",\"prototype\":\"p\"}");

        var roundTripped = Toon.RoundTrip(doc.RootElement);

        Assert.Equal(doc.RootElement.GetRawText(), roundTripped.GetRawText());
    }
}
