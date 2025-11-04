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
    public required string Raw { get; set; }

    /// <summary>
    /// The indentation depth of the line.
    /// </summary>
    public required int Depth { get; set; }

    /// <summary>
    /// The number of spaces of indentation.
    /// </summary>
    public required int Indent { get; set; }

    /// <summary>
    /// The content of the line after removing indentation.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// The line number (1-based).
    /// </summary>
    public required int LineNumber { get; set; }
}

/// <summary>
/// Information about blank lines in TOON input.
/// </summary>
internal class BlankLineInfo
{
    /// <summary>
    /// The line number (1-based).
    /// </summary>
    public required int LineNumber { get; set; }

    /// <summary>
    /// The number of spaces of indentation.
    /// </summary>
    public required int Indent { get; set; }

    /// <summary>
    /// The indentation depth.
    /// </summary>
    public required int Depth { get; set; }
}

/// <summary>
/// Result of parsing array header information.
/// </summary>
internal class ArrayHeaderParseResult
{
    /// <summary>
    /// The parsed header information.
    /// </summary>
    public required ArrayHeaderInfo Header { get; set; }

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
        return element.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null;
    }

    /// <summary>
    /// Gets the value of a JsonElement as an object, handling all supported types.
    /// </summary>
    public static object? GetValue(this JsonElement element)
    {
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
    }
}