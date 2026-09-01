using System.Text.Json;
using ToonFormat.Shared;

namespace ToonFormat.Encode;

/// <summary>
/// Utilities for encoding primitive values and creating headers.
/// </summary>
internal static class Primitives
{
    /// <summary>
    /// Encodes a primitive JsonElement as a TOON string.
    /// </summary>
    /// <param name="element">The primitive element to encode.</param>
    /// <param name="delimiter">The delimiter character for context.</param>
    /// <returns>The encoded string representation.</returns>
    public static string EncodePrimitive(JsonElement element, char delimiter)
    {
        return LiteralUtils.FormatPrimitive(element, delimiter);
    }

    /// <summary>
    /// Encodes a key name, quoting it if necessary.
    /// </summary>
    /// <param name="key">The key name to encode.</param>
    /// <returns>The encoded key name.</returns>
    public static string EncodeKey(string key)
    {
        return StringUtils.EscapeString(key);
    }

    /// <summary>
    /// Formats a header for arrays in TOON format.
    /// </summary>
    /// <param name="length">The array length.</param>
    /// <param name="key">Optional key name.</param>
    /// <param name="delimiter">The delimiter character.</param>
    /// <param name="lengthMarker">Optional length marker character.</param>
    /// <param name="fields">Optional field names for tabular arrays.</param>
    /// <returns>The formatted header string.</returns>
    public static string FormatHeader(int length, string? key = null, char delimiter = Constants.DefaultDelimiter, 
        char? lengthMarker = null, string[]? fields = null)
    {
        var parts = new List<string>();

        // Add key if present
        if (!string.IsNullOrEmpty(key))
        {
            parts.Add(EncodeKey(key));
        }

        // Format length with optional marker and delimiter suffix
        string lengthPart = lengthMarker.HasValue ? $"{lengthMarker}{length}" : length.ToString();
        
        // Add delimiter suffix if not default
        string delimiterSuffix = "";
        if (delimiter != Constants.DefaultDelimiter)
        {
            delimiterSuffix = delimiter.ToString();
        }
        
        parts.Add($"[{lengthPart}{delimiterSuffix}]");

        // Add fields if present
        if (fields != null && fields.Length > 0)
        {
            string fieldList = string.Join(",", fields.Select(EncodeKey));
            parts.Add($"{{{fieldList}}}");
        }

        return string.Join("", parts) + ":";
    }

    /// <summary>
    /// Encodes and joins multiple primitive values with a delimiter.
    /// </summary>
    /// <param name="elements">The primitive elements to encode and join.</param>
    /// <param name="delimiter">The delimiter to use.</param>
    /// <returns>The joined string.</returns>
    public static string EncodeAndJoinPrimitives(JsonElement[] elements, char delimiter)
    {
        return LiteralUtils.FormatAndJoinPrimitives(elements, delimiter);
    }

    /// <summary>
    /// Formats an inline array line with optional key and length marker.
    /// </summary>
    /// <param name="elements">The array elements.</param>
    /// <param name="delimiter">The delimiter to use.</param>
    /// <param name="key">Optional key name.</param>
    /// <param name="lengthMarker">Optional length marker.</param>
    /// <returns>The formatted line.</returns>
    public static string FormatInlineArrayLine(JsonElement[] elements, char delimiter, string? key = null, char? lengthMarker = null)
    {
        string header = FormatHeader(elements.Length, key, delimiter, lengthMarker);
        
        if (elements.Length == 0)
        {
            return header;
        }

        string values = EncodeAndJoinPrimitives(elements, delimiter);
        return $"{header} {values}";
    }
}