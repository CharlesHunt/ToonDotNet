namespace ToonFormat.Tests;

// Tracks progress on TOON_V4.md's implementation order (the v4.0.0+ gap
// list). Phase 0 (pinning the spec baseline) is covered by
// ToonBasicTests.Constants_SpecVersion_MatchesDocumentedBaseline. This
// file starts at phase 1.
public class ToonV4ComplianceTests
{
    // Phase 1, step 2: EncodeOptions.SpecVersion exists. It was
    // originally landed inert (phase 1) defaulting to V3, then — on user
    // request ("I would like the SpecVersion to be wired... giving users
    // the choice over which spec they wish to emit") — wired to
    // genuinely gate every v4-only encoder behavior found so far:
    // nested field groups (§9.3 RFC #46), keyed tabular form (§9.5 RFC
    // #57), and leading-plus numeric-like quoting (§7.2). V3 reproduces
    // true v3.3.2 output (including its known leading-plus gap); V4
    // produces fully-correct v4 output. TOON_V4.md phase 5 then flipped
    // the default to V4, matching Constants.SpecVersion ("4.1.1") — V3
    // is now the explicit opt-in for byte-for-byte v3.3.2 compatibility.
    // For inputs that don't trigger any of the three gated deltas (e.g. a
    // flat, non-nested structure with no numeric-like string values), V3
    // and V4 output is identical — there's no OTHER difference between
    // them yet, since the remaining decoder-only v4 items (comments,
    // strict-mode tightenings, LegacyCompatibility-gated conflicts) have
    // no EncodeOptions surface. See ToonEncoderTests.cs and the dedicated
    // V3-vs-V4 tests throughout this file (search
    // "V4SpecVersion"/"V3SpecVersion") for the actual behavior
    // differences this gates.
    [Fact]
    public void EncodeOptions_SpecVersion_DefaultsToV4()
    {
        var options = new EncodeOptions();

        Assert.Equal(ToonSpecVersion.V4, options.SpecVersion);
    }

    [Fact]
    public void EncodeOptions_SpecVersion_ForInputWithNoV3V4Delta_ProducesIdenticalOutput()
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
    public void Encode_ArrayOfObjectsWithNestedUniformColumn_V4SpecVersion_UsesNestedFieldGroup()
    {
        // Nested field groups are a v4.0.0-only grammar addition (RFC
        // #46) — a v3.3.2 decoder cannot parse "customer{name,country}"
        // at all, so this output shape is only emitted when the caller
        // explicitly opts into V4 (see the SpecVersion-wiring note below
        // and the V3-fallback test immediately following).
        var data = new[]
        {
            new { id = 1, customer = new { name = "Ada", country = "DK" }, total = 99 },
            new { id = 2, customer = new { name = "Bob", country = "UK" }, total = 149 }
        };

        string result = Toon.Encode(data, new EncodeOptions { SpecVersion = ToonSpecVersion.V4 });

        Assert.Contains("[2]{id,customer{name,country},total}:", result);
        Assert.Contains("1,Ada,DK,99", result);
        Assert.Contains("2,Bob,UK,149", result);
    }

    // SpecVersion wiring (added after this phase's initial landing, on
    // user request "give users the choice over which spec they wish to
    // emit"): EncodeOptions.SpecVersion defaults to V3, and V3 now
    // genuinely reproduces v3.3.2-shaped output — nested field groups
    // and keyed tabular form (v4.0.0-only grammar) never activate under
    // V3, falling back exactly to how this array would have encoded
    // before phase 2 existed. This is a structural-interop distinction,
    // not merely cosmetic: a v3-only decoder cannot parse the v4 header
    // shapes at all.
    [Fact]
    public void Encode_ArrayOfObjectsWithNestedUniformColumn_V3SpecVersion_FallsBackToListForm()
    {
        var data = new[]
        {
            new { id = 1, customer = new { name = "Ada", country = "DK" }, total = 99 },
            new { id = 2, customer = new { name = "Bob", country = "UK" }, total = 149 }
        };

        string result = Toon.Encode(data, new EncodeOptions { SpecVersion = ToonSpecVersion.V3 });

        Assert.DoesNotContain("customer{", result);
        Assert.Contains("- id: 1", result);
    }

    [Fact]
    public void Encode_ArrayOfObjectsWithNestedUniformColumn_DefaultOptions_UsesNestedFieldGroup()
    {
        // Regression guard: TOON_V4.md phase 5 flipped EncodeOptions.
        // SpecVersion's default to V4, so default EncodeOptions() must
        // now behave identically to explicit V4, not V3.
        var data = new[]
        {
            new { id = 1, customer = new { name = "Ada", country = "DK" }, total = 99 },
            new { id = 2, customer = new { name = "Bob", country = "UK" }, total = 149 }
        };

        Assert.Equal(
            Toon.Encode(data, new EncodeOptions { SpecVersion = ToonSpecVersion.V4 }),
            Toon.Encode(data));
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
    public void Encode_ObjectOfUniformObjects_V4SpecVersion_UsesKeyedTabularForm()
    {
        // Keyed tabular form (v4.0.0 RFC #57) reuses the bracket/braces
        // header shape with a literal colon inside the brackets
        // ("[2:]") that has no v3.3.2 meaning at all — only emitted when
        // the caller explicitly opts into V4.
        var data = new
        {
            users = new Dictionary<string, object>
            {
                ["alice"] = new { age = 30, city = "Berlin" },
                ["bob"] = new { age = 25, city = "Oslo" }
            }
        };

        string result = Toon.Encode(data, new EncodeOptions { SpecVersion = ToonSpecVersion.V4 });

        Assert.Contains("users[2:]{age,city}:", result);
        Assert.Contains("alice: 30,Berlin", result);
        Assert.Contains("bob: 25,Oslo", result);
    }

    [Fact]
    public void Encode_ObjectOfUniformObjects_V3SpecVersion_FallsBackToNestedObjectForm()
    {
        var data = new
        {
            users = new Dictionary<string, object>
            {
                ["alice"] = new { age = 30, city = "Berlin" },
                ["bob"] = new { age = 25, city = "Oslo" }
            }
        };

        string result = Toon.Encode(data, new EncodeOptions { SpecVersion = ToonSpecVersion.V3 });

        Assert.DoesNotContain("[2:]", result);
        Assert.Contains("alice:", result);
        Assert.Contains("age: 30", result);
    }

    [Fact]
    public void Encode_ObjectOfUniformObjects_DefaultOptions_UsesKeyedTabularForm()
    {
        // Regression guard: TOON_V4.md phase 5 flipped EncodeOptions.
        // SpecVersion's default to V4, so default EncodeOptions() must
        // now behave identically to explicit V4, not V3.
        var data = new
        {
            users = new Dictionary<string, object>
            {
                ["alice"] = new { age = 30, city = "Berlin" },
                ["bob"] = new { age = 25, city = "Oslo" }
            }
        };

        Assert.Equal(
            Toon.Encode(data, new EncodeOptions { SpecVersion = ToonSpecVersion.V4 }),
            Toon.Encode(data));
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

    // Phase 3, step 11: v4.0.0 §12 makes indentation depth jumps (a line
    // more than one level deeper than its enclosing scope), over-indented
    // lines (deeper than the enclosing scope's content depth when the
    // preceding line didn't open a scope), and trailing root content
    // (§5, any content after a completed root form) strict-mode errors —
    // decoders "MUST NOT silently discard" them. Before this fix, all
    // three were silently dropped: whichever recursive decode step first
    // hit a line at an unexpected depth simply stopped and returned,
    // leaving the offending (and anything after it) unconsumed with
    // nothing checking that the whole document was actually consumed.
    // Fixed with one general check rather than three special cases: the
    // top-level decode entry point now asserts (in strict mode) that the
    // line cursor is fully exhausted once decoding finishes — any
    // leftover line, regardless of why the recursive descent stopped
    // short of it, is exactly the "silently discarded" content spec
    // §12/§5 forbids.
    [Fact]
    public void Decode_DepthJumpDirectlyUnderObjectField_StrictMode_Throws()
    {
        // "b: 1" jumps straight from depth 0 to depth 2 under "a:",
        // skipping depth 1 entirely.
        string toon = "a:\n    b: 1";

        Assert.Throws<InvalidOperationException>(() => Toon.Decode(toon));
    }

    [Fact]
    public void Decode_DepthJumpDirectlyUnderObjectField_NonStrictMode_DoesNotThrow()
    {
        string toon = "a:\n    b: 1";

        var options = new DecodeOptions { Strict = false };

        var result = Toon.Decode(toon, options);

        Assert.NotEqual(default, result.ValueKind);
    }

    [Fact]
    public void Decode_OverIndentedLineAfterPrimitiveField_StrictMode_Throws()
    {
        // "a: 1" is a primitive value line — it doesn't open a nested
        // scope, so "b: 2" indented one level deeper than it is invalid,
        // not an implicit sibling.
        string toon = "a: 1\n  b: 2";

        Assert.Throws<InvalidOperationException>(() => Toon.Decode(toon));
    }

    [Fact]
    public void Decode_OverIndentedLineAfterPrimitiveField_NonStrictMode_DoesNotThrow()
    {
        string toon = "a: 1\n  b: 2";

        var options = new DecodeOptions { Strict = false };

        var result = Toon.Decode(toon, options);

        Assert.NotEqual(default, result.ValueKind);
    }

    [Fact]
    public void Decode_OverIndentedLineInsideListItem_StrictMode_Throws()
    {
        // The list item's own continuation fields belong at hyphenDepth+1
        // (depth 2 here); "extra: 2" at depth 3 over-shoots that.
        string toon = "items[1]:\n  - a: 1\n      extra: 2";

        Assert.Throws<InvalidOperationException>(() => Toon.Decode(toon));
    }

    [Fact]
    public void Decode_TrailingContentAfterCompletedRootArray_StrictMode_Throws()
    {
        string toon = "[2]: 1,2\nextra: 1";

        Assert.Throws<InvalidOperationException>(() => Toon.Decode(toon));
    }

    [Fact]
    public void Decode_TrailingContentAfterCompletedRootArray_NonStrictMode_DoesNotThrow()
    {
        string toon = "[2]: 1,2\nextra: 1";

        var options = new DecodeOptions { Strict = false };

        var result = Toon.Decode(toon, options);

        Assert.NotEqual(default, result.ValueKind);
    }

    [Fact]
    public void Decode_NormalMultiLevelNesting_StillDecodesCorrectly()
    {
        // Regression guard: legitimate depth-by-1 nesting at every level
        // must keep working after the "fully consumed" check is added.
        string toon = "a:\n  b:\n    c: 1\nsibling: 2";

        var result = Toon.Decode(toon);

        Assert.Equal(1, result.GetProperty("a").GetProperty("b").GetProperty("c").GetInt32());
        Assert.Equal(2, result.GetProperty("sibling").GetInt32());
    }

    [Fact]
    public void Decode_SingleScalarRootDocument_StillDecodesCorrectly()
    {
        // Regression guard: the single-primitive-root branch must now
        // advance the cursor so the new "fully consumed" check doesn't
        // false-positive on this legitimate one-line document.
        var result = Toon.Decode("hello");

        Assert.Equal("hello", result.GetString());
    }

    [Fact]
    public void Decode_TabularArrayAsFirstFieldOfListItem_StillDecodesCorrectly()
    {
        // Regression guard: spec §10's legitimate depth+2 jump (a tabular
        // array as the first field of a list-item object) must not be
        // mistaken for an invalid depth jump by the new check — its rows
        // are fully consumed by the code that already knows to look for
        // them at that depth, so nothing is left over.
        string toon = "outer[1]:\n  - users[2]{id,name}:\n      1,Alice\n      2,Bob\n    active: true";

        var result = Toon.Decode(toon);

        var item = result.GetProperty("outer").EnumerateArray().First();
        var users = item.GetProperty("users").EnumerateArray().ToArray();
        Assert.Equal(2, users.Length);
        Assert.Equal("Alice", users[0].GetProperty("name").GetString());
        Assert.True(item.GetProperty("active").GetBoolean());
    }

    // Phase 3, step 12: v4.0.0 §7.4 defines the decoder unquoted-key
    // token rule — decoders MUST accept ANY token before the first
    // unquoted colon as a literal key, even when it doesn't match §7.3's
    // unquoted-key encode pattern (^[A-Za-z_][A-Za-z0-9_.]*$). This is a
    // decode-side acceptance widening, not a restriction: strict mode
    // must NOT reject unusual unquoted keys. Already correctly
    // implemented — ParseUnquotedKey (ToonParser.cs) takes everything up
    // to the first colon as the key literal with no pattern validation
    // at all, in either object fields, array-header keys, or tabular
    // field names. Also verified: the quoted-token boundary rule (a
    // quoted key's closing quote MUST be the token's last character) is
    // enforced unconditionally, not just in strict mode. These tests
    // make that already-correct behavior intentional rather than
    // incidental — no code change was needed for this step.
    [Fact]
    public void Decode_UnquotedKeyWithHyphen_StrictMode_Accepted()
    {
        var result = Toon.Decode("foo-bar: 1");

        Assert.Equal(1, result.GetProperty("foo-bar").GetInt32());
    }

    [Fact]
    public void Decode_UnquotedArrayHeaderKeyWithHyphen_StrictMode_Accepted()
    {
        var result = Toon.Decode("foo-bar[2]: 1,2");

        Assert.Equal(new[] { 1, 2 }, result.GetProperty("foo-bar").EnumerateArray().Select(e => e.GetInt32()));
    }

    [Fact]
    public void Decode_UnquotedTabularFieldNameNotMatchingIdentifierPattern_StrictMode_Accepted()
    {
        var result = Toon.Decode("items[1]{2key}:\n  5");

        var item = result.GetProperty("items").EnumerateArray().First();
        Assert.Equal(5, item.GetProperty("2key").GetInt32());
    }

    [Fact]
    public void Decode_UnquotedKeyWithInternalSpace_StrictMode_Accepted()
    {
        var result = Toon.Decode("foo bar: 1");

        Assert.Equal(1, result.GetProperty("foo bar").GetInt32());
    }

    [Fact]
    public void Decode_QuotedKeyWithCharactersAfterClosingQuote_Throws()
    {
        // Spec §7.4: a quoted token's closing quote MUST be the token's
        // last character; anything after it MUST error — unconditionally,
        // not just in strict mode.
        Assert.Throws<InvalidOperationException>(() => Toon.Decode("\"foo\"extra: 1"));
    }

    [Fact]
    public void Decode_QuotedKeyWithCharactersAfterClosingQuote_NonStrictMode_StillThrows()
    {
        var options = new DecodeOptions { Strict = false };

        Assert.Throws<InvalidOperationException>(() => Toon.Decode("\"foo\"extra: 1", options));
    }

    // Phase 3, step 13: v4.0.0 §4 requires strict decoders to reject
    // ill-formed UTF-8 byte input (invalid or truncated sequences, or
    // bytes encoding surrogate code points) rather than silently
    // replacing it with U+FFFD. This is a genuine gap: the byte-level
    // decode paths (ToonStream.cs) used the shared Encoding.UTF8
    // instance, which has a replacement fallback and never throws.
    // Fixed by resolving to a throwing UTF8Encoding instead whenever the
    // caller didn't explicitly supply an Encoding and DecodeOptions.Strict
    // is true (the default); an explicitly supplied Encoding is always
    // respected as-is, since the caller has already taken control of
    // byte-decoding behavior. Decoders that accept host strings (the
    // string/TextReader overloads) are outside this rule per spec, since
    // .NET strings are already-decoded UTF-16, not raw bytes.
    [Fact]
    public void DecodeStream_IllFormedUtf8Bytes_StrictMode_Throws()
    {
        // 0xC0 0x80 is an overlong encoding, never valid UTF-8.
        byte[] illFormedUtf8 = { 0xC0, 0x80 };
        using var stream = new MemoryStream(illFormedUtf8);

        Assert.Throws<InvalidOperationException>(() => Toon.Decode(stream));
    }

    [Fact]
    public void DecodeStream_IllFormedUtf8Bytes_NonStrictMode_DoesNotThrow()
    {
        byte[] illFormedUtf8 = { 0xC0, 0x80 };
        using var stream = new MemoryStream(illFormedUtf8);

        var result = Toon.Decode(stream, new DecodeOptions { Strict = false });

        Assert.NotEqual(default, result.ValueKind);
    }

    [Fact]
    public void DecodeStreamGeneric_IllFormedUtf8Bytes_StrictMode_Throws()
    {
        byte[] illFormedUtf8 = { 0xC0, 0x80 };
        using var stream = new MemoryStream(illFormedUtf8);

        Assert.Throws<InvalidOperationException>(() => Toon.Decode<string>(stream));
    }

    [Fact]
    public async Task DecodeStreamAsync_IllFormedUtf8Bytes_StrictMode_Throws()
    {
        byte[] illFormedUtf8 = { 0xC0, 0x80 };
        using var stream = new MemoryStream(illFormedUtf8);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Toon.DecodeAsync(stream));
    }

    [Fact]
    public async Task DecodeStreamAsyncGeneric_IllFormedUtf8Bytes_StrictMode_Throws()
    {
        byte[] illFormedUtf8 = { 0xC0, 0x80 };
        using var stream = new MemoryStream(illFormedUtf8);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Toon.DecodeAsync<string>(stream));
    }

    [Fact]
    public void DecodeStream_ExplicitEncodingProvided_StrictMode_DoesNotOverrideCallerChoice()
    {
        // An explicitly supplied Encoding is respected as-is, even in
        // strict mode: the caller has already taken control of
        // byte-decoding behavior, so this must not silently throw where
        // the caller's own encoding would have used its own fallback.
        byte[] illFormedUtf8 = { 0xC0, 0x80 };
        using var stream = new MemoryStream(illFormedUtf8);

        var result = Toon.Decode(stream, options: null, encoding: System.Text.Encoding.UTF8);

        Assert.NotEqual(default, result.ValueKind);
    }

    [Fact]
    public void DecodeStream_ValidUtf8Bytes_StrictMode_StillDecodesCorrectly()
    {
        // Regression guard: normal (including non-ASCII multi-byte)
        // content must still decode correctly through the new throwing
        // encoding.
        byte[] validUtf8 = System.Text.Encoding.UTF8.GetBytes("name: café");
        using var stream = new MemoryStream(validUtf8);

        var result = Toon.Decode(stream);

        Assert.Equal("café", result.GetProperty("name").GetString());
    }

    // Phase 4, step 15: v4.0.0 §7.2 requires encoders to quote
    // leading-plus numeric-like strings (the numeric-like pattern's sign
    // class becomes [+-] instead of just [-]). .NET's
    // NumberStyles.Integer/Float both accept a leading '+' via
    // AllowLeadingSign, so an unquoted "+5" silently decodes back as the
    // number 5 on any decoder — a real round-trip hazard, but one that
    // v3.3.2's own (narrower) regex accepts as a known gap. After
    // EncodeOptions.SpecVersion was wired to genuinely gate v3-vs-v4
    // output (user request, post-initial-phase-4 landing), this became
    // version-gated like the structural v4 features: V3 (the default)
    // reproduces true v3.3.2 behavior including this gap; V4 closes it.
    [Fact]
    public void Encode_StringLeadingPlusInteger_V4SpecVersion_IsQuoted()
    {
        var data = new { x = "+5" };

        string result = Toon.Encode(data, new EncodeOptions { SpecVersion = ToonSpecVersion.V4 });

        Assert.Contains("x: \"+5\"", result);
    }

    [Fact]
    public void Encode_StringLeadingPlusDecimal_V4SpecVersion_IsQuoted()
    {
        var data = new { x = "+3.14" };

        string result = Toon.Encode(data, new EncodeOptions { SpecVersion = ToonSpecVersion.V4 });

        Assert.Contains("x: \"+3.14\"", result);
    }

    [Fact]
    public void Encode_StringLeadingPlusExponent_V4SpecVersion_IsQuoted()
    {
        var data = new { x = "+5e10" };

        string result = Toon.Encode(data, new EncodeOptions { SpecVersion = ToonSpecVersion.V4 });

        Assert.Contains("x: \"+5e10\"", result);
    }

    [Fact]
    public void Encode_StringLeadingPlusInteger_V3SpecVersion_RemainsUnquoted()
    {
        var data = new { x = "+5" };

        string result = Toon.Encode(data, new EncodeOptions { SpecVersion = ToonSpecVersion.V3 });

        Assert.Contains("x: +5", result);
    }

    [Fact]
    public void Encode_StringLeadingPlusInteger_DefaultOptions_IsQuoted()
    {
        // Regression guard: TOON_V4.md phase 5 flipped EncodeOptions.
        // SpecVersion's default to V4, so default EncodeOptions() must
        // now behave identically to explicit V4, not V3.
        var data = new { x = "+5" };

        Assert.Equal(
            Toon.Encode(data, new EncodeOptions { SpecVersion = ToonSpecVersion.V4 }),
            Toon.Encode(data));
    }

    [Fact]
    public void RoundTrip_StringLeadingPlusInteger_V4SpecVersion_PreservesStringType()
    {
        var data = new { x = "+5" };
        var encodeOptions = new EncodeOptions { SpecVersion = ToonSpecVersion.V4 };

        var roundTripped = Toon.RoundTrip(data, encodeOptions);

        Assert.Equal(System.Text.Json.JsonValueKind.String, roundTripped.GetProperty("x").ValueKind);
        Assert.Equal("+5", roundTripped.GetProperty("x").GetString());
    }

    [Fact]
    public void RoundTrip_StringLeadingPlusInteger_V3SpecVersion_KnownGap_CorruptsToNumber()
    {
        // Documents the known, spec-acknowledged v3.3.2 gap this fix
        // closes under V4: under genuine V3 output, "+5" round-trips
        // incorrectly as the number 5, not the string "+5". This is
        // intentional — V3 means "reproduce v3.3.2," gap included — not
        // a regression to chase.
        var data = new { x = "+5" };
        var encodeOptions = new EncodeOptions { SpecVersion = ToonSpecVersion.V3 };

        var roundTripped = Toon.RoundTrip(data, encodeOptions);

        Assert.Equal(System.Text.Json.JsonValueKind.Number, roundTripped.GetProperty("x").ValueKind);
        Assert.Equal(5, roundTripped.GetProperty("x").GetInt32());
    }

    [Fact]
    public void Encode_StringLeadingMinusInteger_StillQuotedUnderBothSpecVersions()
    {
        // Regression guard: leading-minus quoting was already part of
        // v3.3.2's own regex, so it must keep working under both V3 and
        // V4 — only the leading-plus behavior differs between them.
        var data = new { x = "-5" };

        Assert.Contains("x: \"-5\"", Toon.Encode(data, new EncodeOptions { SpecVersion = ToonSpecVersion.V3 }));
        Assert.Contains("x: \"-5\"", Toon.Encode(data, new EncodeOptions { SpecVersion = ToonSpecVersion.V4 }));
    }

    [Fact]
    public void Encode_PlusSignedIntegerLiteral_StillEncodesUnquotedAsNumber()
    {
        // Regression guard: an actual numeric value should be unaffected
        // — this fix only changes STRING-value quoting decisions.
        var data = new { x = 5 };

        string result = Toon.Encode(data);

        Assert.Contains("x: 5", result);
        Assert.DoesNotContain("\"5\"", result);
    }

    // Phase 4, step 14: v4.0.0 §12 restricts token trimming to exactly
    // U+0020 (promoted to MUST) — tabs, non-breaking space, and every
    // other whitespace category remain part of the token. This is a
    // genuine semantic conflict with the pre-v4 behavior (plain
    // string.Trim(), which strips the broader char.IsWhiteSpace set): the
    // same bytes decode to a different string. Fixed with a new
    // StringUtils.TrimToken(value, legacyCompatibility) helper threaded
    // through every token-extraction point spec §12 names: key tokens,
    // entry-key tokens (§9.5), value tokens (after key-value and
    // array-header colons), and delimiter-separated tokens (row/entry-row
    // cells, tabular field names). DecodeOptions.LegacyCompatibility
    // (default false) opts back into the old broader-whitespace behavior.
    [Fact]
    public void Decode_ValueTokenWithLeadingTab_DefaultMode_PreservesTabInValue()
    {
        string toon = "key: \tvalue";

        var result = Toon.Decode(toon);

        Assert.Equal("\tvalue", result.GetProperty("key").GetString());
    }

    [Fact]
    public void Decode_ValueTokenWithLeadingTab_LegacyCompatibilityMode_StripsTab()
    {
        string toon = "key: \tvalue";

        var result = Toon.Decode(toon, new DecodeOptions { LegacyCompatibility = true });

        Assert.Equal("value", result.GetProperty("key").GetString());
    }

    [Fact]
    public void Decode_KeyTokenWithTrailingTab_DefaultMode_PreservesTabInKey()
    {
        string toon = "key\t: value";

        var result = Toon.Decode(toon);

        Assert.Equal("value", result.GetProperty("key\t").GetString());
    }

    [Fact]
    public void Decode_KeyTokenWithTrailingTab_LegacyCompatibilityMode_StripsTab()
    {
        string toon = "key\t: value";

        var result = Toon.Decode(toon, new DecodeOptions { LegacyCompatibility = true });

        Assert.Equal("value", result.GetProperty("key").GetString());
    }

    [Fact]
    public void Decode_TabularRowCellWithLeadingTab_DefaultMode_PreservesTabInCell()
    {
        string toon = "items[1]{a,b}:\n  x,\ty";

        var result = Toon.Decode(toon);

        var item = result.GetProperty("items").EnumerateArray().First();
        Assert.Equal("\ty", item.GetProperty("b").GetString());
    }

    [Fact]
    public void Decode_TabularRowCellWithLeadingTab_LegacyCompatibilityMode_StripsTab()
    {
        string toon = "items[1]{a,b}:\n  x,\ty";

        var result = Toon.Decode(toon, new DecodeOptions { LegacyCompatibility = true });

        var item = result.GetProperty("items").EnumerateArray().First();
        Assert.Equal("y", item.GetProperty("b").GetString());
    }

    [Fact]
    public void Decode_KeyedTabularEntryCellWithLeadingTab_DefaultMode_PreservesTabInCell()
    {
        string toon = "table[2:]{v}:\n  a: \tx\n  b: y";

        var result = Toon.Decode(toon);

        Assert.Equal("\tx", result.GetProperty("table").GetProperty("a").GetProperty("v").GetString());
    }

    [Fact]
    public void Decode_KeyedTabularEntryCellWithLeadingTab_LegacyCompatibilityMode_StripsTab()
    {
        string toon = "table[2:]{v}:\n  a: \tx\n  b: y";

        var result = Toon.Decode(toon, new DecodeOptions { LegacyCompatibility = true });

        Assert.Equal("x", result.GetProperty("table").GetProperty("a").GetProperty("v").GetString());
    }

    [Fact]
    public void Decode_InlineArrayHeaderValueWithLeadingTab_DefaultMode_PreservesTabInFirstElement()
    {
        string toon = "items[1]: \tx";

        var result = Toon.Decode(toon);

        Assert.Equal("\tx", result.GetProperty("items").EnumerateArray().First().GetString());
    }

    [Fact]
    public void Decode_InlineArrayHeaderValueWithLeadingTab_LegacyCompatibilityMode_StripsTab()
    {
        string toon = "items[1]: \tx";

        var result = Toon.Decode(toon, new DecodeOptions { LegacyCompatibility = true });

        Assert.Equal("x", result.GetProperty("items").EnumerateArray().First().GetString());
    }

    [Fact]
    public void Decode_TabularFieldNameWithTrailingTab_DefaultMode_PreservesTabInFieldName()
    {
        string toon = "items[1]{a\t,b}:\n  1,2";

        var result = Toon.Decode(toon);

        var item = result.GetProperty("items").EnumerateArray().First();
        Assert.Equal(1, item.GetProperty("a\t").GetInt32());
    }

    [Fact]
    public void Decode_TabularFieldNameWithTrailingTab_LegacyCompatibilityMode_StripsTab()
    {
        string toon = "items[1]{a\t,b}:\n  1,2";

        var result = Toon.Decode(toon, new DecodeOptions { LegacyCompatibility = true });

        var item = result.GetProperty("items").EnumerateArray().First();
        Assert.Equal(1, item.GetProperty("a").GetInt32());
    }

    [Fact]
    public void Decode_SingleScalarRootDocumentWithTrailingTab_DefaultMode_PreservesTab()
    {
        // A leading tab on a line is out of scope here — it's already
        // (correctly, and separately from this fix) rejected by the
        // scanner's tabs-in-indentation strict check, since a decoder
        // can't tell whether a leading tab was meant as indentation or
        // as literal content. A trailing tab has no such ambiguity.
        string toon = "hello\t";

        var result = Toon.Decode(toon);

        Assert.Equal("hello\t", result.GetString());
    }

    [Fact]
    public void Decode_ValueTokenWithNonBreakingSpace_DefaultMode_PreservesNonBreakingSpace()
    {
        // Spec §12 explicitly calls out non-breaking space (U+00A0) as an
        // example of a whitespace category that remains part of the token.
        string toon = "key: value ";

        var result = Toon.Decode(toon);

        Assert.Equal("value ", result.GetProperty("key").GetString());
    }

    [Fact]
    public void Decode_ValueTokenWithOnlyRegularSpacePadding_BothModes_StripsSpace()
    {
        // Regression guard: ordinary U+0020 padding must still be
        // stripped in both modes — this option only concerns which
        // characters BEYOND U+0020 get trimmed.
        string toon = "key:   value  ";

        var defaultResult = Toon.Decode(toon);
        var legacyResult = Toon.Decode(toon, new DecodeOptions { LegacyCompatibility = true });

        Assert.Equal("value", defaultResult.GetProperty("key").GetString());
        Assert.Equal("value", legacyResult.GetProperty("key").GetString());
    }

    // Phase 4, step 16: v4.1.0 promotes a "misplaced scalar" line (a bare
    // primitive token that is not a list item, array header, or
    // key-value line, and is not the document's single root primitive)
    // to a structural error in BOTH strict and non-strict modes —
    // previously only strict mode rejected it. Verified this codebase's
    // baseline was actually more lenient than the v4.0.0-and-earlier
    // spec assumed: DecodeListArray's "line at list-item depth without a
    // '- ' prefix" branch silently accepted such a line as an implicit
    // primitive list item in BOTH modes already, with no strict-mode
    // baseline rejection to begin with. Fixed by making this throw
    // unconditionally unless DecodeOptions.LegacyCompatibility opts back
    // into the old lenient behavior (closing the pre-v4 baseline gap and
    // implementing the v4.1 "also reject in non-strict mode" tightening
    // in a single change, since there was no intermediate baseline to
    // preserve for this codebase specifically).
    [Fact]
    public void Decode_MisplacedScalarInList_StrictMode_Throws()
    {
        string toon = "items[2]:\n  - a\n  bare-scalar";

        Assert.Throws<InvalidOperationException>(() => Toon.Decode(toon));
    }

    [Fact]
    public void Decode_MisplacedScalarInList_NonStrictMode_AlsoThrows()
    {
        // The v4.1 tightening: this must error in non-strict mode too,
        // not just strict.
        string toon = "items[2]:\n  - a\n  bare-scalar";

        var options = new DecodeOptions { Strict = false };

        Assert.Throws<InvalidOperationException>(() => Toon.Decode(toon, options));
    }

    [Fact]
    public void Decode_MisplacedScalarInList_LegacyCompatibilityMode_StrictFalse_PreservesOldLenientBehavior()
    {
        string toon = "items[2]:\n  - a\n  bare-scalar";

        var options = new DecodeOptions { Strict = false, LegacyCompatibility = true };

        var result = Toon.Decode(toon, options);

        var items = result.GetProperty("items").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(new[] { "a", "bare-scalar" }, items);
    }

    [Fact]
    public void Decode_ListWithAllProperListItems_StillDecodesCorrectly()
    {
        // Regression guard: a well-formed list (every item introduced by
        // "- ") must be completely unaffected by this fix.
        string toon = "items[2]:\n  - a\n  - b";

        var result = Toon.Decode(toon);

        var items = result.GetProperty("items").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(new[] { "a", "b" }, items);
    }
}
