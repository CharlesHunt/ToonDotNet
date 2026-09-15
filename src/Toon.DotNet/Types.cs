using System.Text.Json;

namespace ToonFormat;

/// <summary>
/// The TOON grammar variant to target. See TOON_V4.md for the v4.0.0+
/// feature/behavior gap list this drives.
/// </summary>
public enum ToonSpecVersion
{
    /// <summary>
    /// TOON spec v3.x. Reproduces true v3.3.2 output for the behaviors
    /// this enum gates: no nested field groups, no keyed tabular form,
    /// and the narrower (leading '-' only) numeric-like quoting pattern
    /// — the known v3.3.2 leading-plus round-trip gap included. Opt into
    /// this when byte-for-byte v3.3.2 compatibility with a downstream
    /// v3-only consumer matters more than the v4 fixes.
    /// </summary>
    V3,

    /// <summary>
    /// TOON spec v4.0.0+ (the line this library targets as of
    /// <see cref="Constants.SpecVersion"/>, and <see cref="EncodeOptions.SpecVersion"/>'s
    /// default). See TOON_V4.md for implementation status; encoding
    /// gates nested field groups (§9.3 RFC #46), keyed tabular form
    /// (§9.5 RFC #57), and leading-plus numeric-like quoting (§7.2)
    /// behind this value.
    /// </summary>
    V4
}

/// <summary>
/// Configuration options for encoding values to TOON format.
/// </summary>
public class EncodeOptions
{
    /// <summary>
    /// Number of spaces per indentation level.
    /// </summary>
    public int Indent { get; set; } = 2;

    /// <summary>
    /// Delimiter to use for tabular array rows and inline primitive arrays.
    /// </summary>
    public char Delimiter { get; set; } = Constants.DefaultDelimiter;

    /// <summary>
    /// Optional marker to prefix array lengths in headers.
    /// When set to '#', arrays render as [#N] instead of [N].
    /// </summary>
    public char? LengthMarker { get; set; }

    /// <summary>
    /// The TOON grammar variant to encode. Defaults to
    /// <see cref="ToonSpecVersion.V4"/>, matching <see cref="Constants.SpecVersion"/>
    /// — encoding uses nested field groups, keyed tabular form, and the
    /// wider leading-plus numeric-like quoting pattern by default. Set to
    /// <see cref="ToonSpecVersion.V3"/> to instead reproduce true v3.3.2
    /// output (no nested field groups, no keyed tabular form, the
    /// narrower leading-'-'-only quoting pattern) for compatibility with
    /// a downstream v3-only consumer. See TOON_V4.md's "Version-aware
    /// EncodeOptions / DecodeOptions" section for the full list of gated
    /// behaviors.
    /// </summary>
    public ToonSpecVersion SpecVersion { get; set; } = ToonSpecVersion.V4;
}

/// <summary>
/// Configuration options for decoding TOON format to values.
/// </summary>
public class DecodeOptions
{
    /// <summary>
    /// Number of spaces per indentation level.
    /// </summary>
    public int Indent { get; set; } = 2;

    /// <summary>
    /// When true, enforce strict validation of array lengths and tabular row counts.
    /// </summary>
    public bool Strict { get; set; } = true;

    /// <summary>
    /// When true, opts back into the pre-v4 decoder behavior for the
    /// small set of TOON v4 semantic changes that genuinely conflict
    /// with v3 behavior for the same input: token trimming scope (plain
    /// whitespace trimming instead of spec §12's U+0020-only rule) and
    /// the v4.1 misplaced-scalar rule (tolerating a bare, non-"- "-prefixed
    /// line inside a list instead of erroring). Defaults to <c>false</c>
    /// (correct v4 behavior). The decoder does not use a version
    /// selector to decide what grammar it understands — see TOON_V4.md's
    /// "superset grammar, not per-version detection" section — this flag
    /// exists only for this narrow set of genuine conflicts, not general
    /// v3-vs-v4 selection.
    /// </summary>
    public bool LegacyCompatibility { get; set; } = false;
}

/// <summary>
/// A field in a tabular header (spec §9.3 arrays-of-objects, §9.5
/// keyed-object tables). A leaf field (<see cref="Children"/> null or
/// empty) maps directly to one row cell. A field with children is a
/// nested field group (v4.0.0 RFC #46, e.g. <c>customer{name,country}</c>)
/// whose row cells are the depth-first, pre-order walk of its own
/// children — recursively, so nesting depth is unbounded per spec.
/// </summary>
internal class TabularField
{
#if NETSTANDARD2_0
    public string Name { get; set; }
#else
    public required string Name { get; set; }
#endif

    /// <summary>
    /// Null (or empty) for a leaf field; the nested field list for a
    /// nested field group.
    /// </summary>
    public TabularField[]? Children { get; set; }
}

/// <summary>
/// Information about array headers parsed from TOON format.
/// </summary>
internal class ArrayHeaderInfo
{
    /// <summary>
    /// The key name for the array (if any).
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// The declared length of the array.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// The delimiter character used in the array.
    /// </summary>
    public char Delimiter { get; set; }

    /// <summary>
    /// Field list for tabular arrays (if any), including any nested
    /// field groups (spec §9.3, v4.0.0 RFC #46).
    /// </summary>
    public TabularField[]? Fields { get; set; }

    /// <summary>
    /// Whether the array header includes a length marker (#).
    /// </summary>
    public bool HasLengthMarker { get; set; }

    /// <summary>
    /// True when the bracket segment used the keyed-tabular grammar
    /// (spec §6 keyed-seg, v4.0.0 RFC #57: "[" length ":" [delimsym] "]"),
    /// e.g. "users[2:]{age,city}:". A keyed-tabular header decodes to an
    /// <em>object</em> whose entries carry their own keys (§9.5), not an
    /// array — despite reusing the same bracket/braces header shape.
    /// </summary>
    public bool IsKeyedTabular { get; set; }
}

/// <summary>
/// Represents a parsed line from TOON input.
/// </summary>
internal class ParsedLine
{
    /// <summary>
    /// The raw line content.
    /// </summary>
#if NETSTANDARD2_0
    public string Raw { get; set; }
#else
    public required string Raw { get; set; }
#endif

    /// <summary>
    /// The indentation depth of the line.
    /// </summary>
#if NETSTANDARD2_0
    public int Depth { get; set; }
#else
    public required int Depth { get; set; }
#endif

    /// <summary>
    /// The number of spaces of indentation.
    /// </summary>
#if NETSTANDARD2_0
    public int Indent { get; set; }
#else
    public required int Indent { get; set; }
#endif

    /// <summary>
    /// The content of the line after removing indentation.
    /// </summary>
#if NETSTANDARD2_0
    public string Content { get; set; }
#else
    public required string Content { get; set; }
#endif

    /// <summary>
    /// The line number (1-based).
    /// </summary>
#if NETSTANDARD2_0
    public int LineNumber { get; set; }
#else
    public required int LineNumber { get; set; }
#endif
}

/// <summary>
/// Information about blank lines in TOON input.
/// </summary>
internal class BlankLineInfo
{
    /// <summary>
    /// The line number (1-based).
    /// </summary>
#if NETSTANDARD2_0
    public int LineNumber { get; set; }
#else
    public required int LineNumber { get; set; }
#endif

    /// <summary>
    /// The number of spaces of indentation.
    /// </summary>
#if NETSTANDARD2_0
    public int Indent { get; set; }
#else
    public required int Indent { get; set; }
#endif

    /// <summary>
    /// The indentation depth.
    /// </summary>
#if NETSTANDARD2_0
    public int Depth { get; set; }
#else
    public required int Depth { get; set; }
#endif
}

/// <summary>
/// Result of parsing array header information.
/// </summary>
internal class ArrayHeaderParseResult
{
    /// <summary>
    /// The parsed header information.
    /// </summary>
#if NETSTANDARD2_0
    public ArrayHeaderInfo Header { get; set; }
#else
    public required ArrayHeaderInfo Header { get; set; }
#endif

    /// <summary>
    /// Inline values parsed from the header line (if any).
    /// </summary>
    public JsonElement[]? InlineValues { get; set; }
}

/// <summary>
/// Extensions for working with JsonElement values.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    /// Checks if a JsonElement represents a primitive value (string, number, boolean, or null).
    /// </summary>
    public static bool IsPrimitive(this JsonElement element)
    {
#if NETSTANDARD2_0
    var vk = element.ValueKind;
    return vk == JsonValueKind.String || vk == JsonValueKind.Number || vk == JsonValueKind.True || vk == JsonValueKind.False || vk == JsonValueKind.Null;
#else
    return element.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null;
#endif
    }

    /// <summary>
    /// Gets the value of a JsonElement as an object, handling all supported types.
    /// </summary>
    public static object? GetValue(this JsonElement element)
    {
#if NETSTANDARD2_0
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt32(out var intValue))
                    return intValue;
                return element.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Object:
                return element;
            case JsonValueKind.Array:
                return element;
            default:
                throw new InvalidOperationException($"Unsupported JsonValueKind: {element.ValueKind}");
        }
#else
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => element,
            JsonValueKind.Array => element,
            _ => throw new InvalidOperationException($"Unsupported JsonValueKind: {element.ValueKind}")
        };
#endif
    }
}