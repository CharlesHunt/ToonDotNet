using System.Text.Json;

namespace ToonFormat;

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
    /// Field names for tabular arrays (if any).
    /// </summary>
    public string[]? Fields { get; set; }

    /// <summary>
    /// Whether the array header includes a length marker (#).
    /// </summary>
    public bool HasLengthMarker { get; set; }
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