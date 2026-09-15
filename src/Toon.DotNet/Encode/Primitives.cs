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
    /// <param name="specVersion">Which TOON grammar's quoting rules to apply — see <see cref="StringUtils.EscapeString"/>.</param>
    /// <returns>The encoded string representation.</returns>
    public static string EncodePrimitive(JsonElement element, char delimiter, ToonSpecVersion specVersion = ToonSpecVersion.V4)
    {
        return LiteralUtils.FormatPrimitive(element, delimiter, specVersion);
    }

    /// <summary>
    /// Encodes a key name, quoting it if necessary.
    /// </summary>
    /// <param name="key">The key name to encode.</param>
    /// <returns>The encoded key name.</returns>
    public static string EncodeKey(string key)
    {
        return StringUtils.EscapeKey(key);
    }

    /// <summary>
    /// Formats a header for arrays in TOON format.
    /// </summary>
    /// <param name="length">The array length.</param>
    /// <param name="key">Optional key name.</param>
    /// <param name="delimiter">The delimiter character.</param>
    /// <param name="lengthMarker">Optional length marker character.</param>
    /// <param name="fields">Optional tabular field list, including any nested field groups.</param>
    /// <returns>The formatted header string.</returns>
    public static string FormatHeader(int length, string? key = null, char delimiter = Constants.DefaultDelimiter,
        char? lengthMarker = null, TabularField[]? fields = null)
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

        // Add fields if present, expanding nested field groups (v4.0.0
        // RFC #46) recursively, e.g. "customer{name,country}".
        if (fields != null && fields.Length > 0)
        {
            parts.Add($"{{{FormatFieldList(fields)}}}");
        }

        return string.Join("", parts) + ":";
    }

    /// <summary>
    /// Formats a keyed-tabular header (spec §6 keyed-seg grammar,
    /// v4.0.0 RFC #57): "key[N:delim?]{fields}:" — note the literal
    /// colon inside the bracket segment, which distinguishes this from a
    /// plain array header and has no length-marker counterpart.
    /// </summary>
    /// <param name="entryCount">The object's entry count (N).</param>
    /// <param name="key">Optional key name (omitted at the document root).</param>
    /// <param name="delimiter">The delimiter character.</param>
    /// <param name="fields">The (required) field list, including any nested field groups.</param>
    public static string FormatKeyedHeader(int entryCount, string? key, char delimiter, TabularField[] fields)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(key))
        {
            parts.Add(EncodeKey(key));
        }

        string delimiterSuffix = delimiter != Constants.DefaultDelimiter ? delimiter.ToString() : "";
        parts.Add($"[{entryCount}:{delimiterSuffix}]");
        parts.Add($"{{{FormatFieldList(fields)}}}");

        return string.Join("", parts) + ":";
    }

    /// <summary>
    /// Formats a tabular field list, rendering nested field groups
    /// recursively.
    /// </summary>
    private static string FormatFieldList(TabularField[] fields)
    {
        return string.Join(",", fields.Select(FormatField));
    }

    /// <summary>
    /// Formats one field: a bare name for a leaf field, or
    /// "name{child,child,...}" for a nested field group.
    /// </summary>
    private static string FormatField(TabularField field)
    {
        string name = EncodeKey(field.Name);
        if (field.Children == null || field.Children.Length == 0)
        {
            return name;
        }

        return $"{name}{{{FormatFieldList(field.Children)}}}";
    }

    /// <summary>
    /// Encodes and joins multiple primitive values with a delimiter.
    /// </summary>
    /// <param name="elements">The primitive elements to encode and join.</param>
    /// <param name="delimiter">The delimiter to use.</param>
    /// <param name="specVersion">Which TOON grammar's quoting rules to apply — see <see cref="StringUtils.EscapeString"/>.</param>
    /// <returns>The joined string.</returns>
    public static string EncodeAndJoinPrimitives(JsonElement[] elements, char delimiter, ToonSpecVersion specVersion = ToonSpecVersion.V4)
    {
        return LiteralUtils.FormatAndJoinPrimitives(elements, delimiter, specVersion);
    }

    /// <summary>
    /// Formats an inline array line with optional key and length marker.
    /// </summary>
    /// <param name="elements">The array elements.</param>
    /// <param name="delimiter">The delimiter to use.</param>
    /// <param name="key">Optional key name.</param>
    /// <param name="lengthMarker">Optional length marker.</param>
    /// <param name="specVersion">Which TOON grammar's quoting rules to apply — see <see cref="StringUtils.EscapeString"/>.</param>
    /// <returns>The formatted line.</returns>
    public static string FormatInlineArrayLine(JsonElement[] elements, char delimiter, string? key = null, char? lengthMarker = null, ToonSpecVersion specVersion = ToonSpecVersion.V4)
    {
        string header = FormatHeader(elements.Length, key, delimiter, lengthMarker);

        if (elements.Length == 0)
        {
            return header;
        }

        string values = EncodeAndJoinPrimitives(elements, delimiter, specVersion);
        return $"{header} {values}";
    }
}