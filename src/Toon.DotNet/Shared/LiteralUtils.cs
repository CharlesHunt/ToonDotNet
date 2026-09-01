using System.Globalization;
using System.Text.Json;

namespace ToonFormat.Shared;

/// <summary>
/// Utility methods for handling literal values in TOON format.
/// </summary>
internal static class LiteralUtils
{
    /// <summary>
    /// Parses a primitive token into a JsonElement.
    /// </summary>
    /// <param name="token">The token to parse.</param>
    /// <returns>A JsonElement representing the parsed value.</returns>
    public static JsonElement ParsePrimitiveToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return JsonDocument.Parse("null").RootElement;

        string trimmed = token.Trim();

        // Handle quoted strings - but validate they are properly quoted
#if NETSTANDARD2_0
        if (trimmed.StartsWith(Constants.DoubleQuote.ToString()))
#else
        if (trimmed.StartsWith(Constants.DoubleQuote))
#endif
        {
            // Validate the string is properly terminated
            int closingQuote = StringUtils.FindClosingQuote(trimmed, 0);
            if (closingQuote == -1 || closingQuote != trimmed.Length - 1)
            {
                throw new InvalidOperationException($"Unterminated or invalid quoted string: {trimmed}");
            }
            
            string unescaped = StringUtils.UnescapeString(trimmed);
            return JsonDocument.Parse(JsonSerializer.Serialize(unescaped)).RootElement;
        }

        // Handle literals
        switch (trimmed)
        {
            case Constants.NullLiteral:
                return JsonDocument.Parse("null").RootElement;
            case Constants.TrueLiteral:
                return JsonDocument.Parse("true").RootElement;
            case Constants.FalseLiteral:
                return JsonDocument.Parse("false").RootElement;
        }

        // Try to parse as number
        if (TryParseNumber(trimmed, out JsonElement numberElement))
        {
            return numberElement;
        }

        // Default to string
        return JsonDocument.Parse(JsonSerializer.Serialize(trimmed)).RootElement;
    }

    /// <summary>
    /// Attempts to parse a string as a number.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="element">The resulting JsonElement if parsing succeeds.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    private static bool TryParseNumber(string value, out JsonElement element)
    {
        // Try integer first
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
        {
            element = JsonDocument.Parse(intValue.ToString()).RootElement;
            return true;
        }

        // Try long
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
        {
            element = JsonDocument.Parse(longValue.ToString()).RootElement;
            return true;
        }

        // Try double
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
        {
            element = JsonDocument.Parse(doubleValue.ToString("G17", CultureInfo.InvariantCulture)).RootElement;
            return true;
        }

        element = default;
        return false;
    }

    /// <summary>
    /// Formats a primitive JsonElement as a TOON string.
    /// </summary>
    /// <param name="element">The JsonElement to format.</param>
    /// <param name="delimiter">The delimiter character (used for escaping if needed).</param>
    /// <returns>The formatted string representation.</returns>
    public static string FormatPrimitive(JsonElement element, char delimiter)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => Constants.NullLiteral,
            JsonValueKind.True => Constants.TrueLiteral,
            JsonValueKind.False => Constants.FalseLiteral,
            JsonValueKind.Number => FormatNumber(element),
            JsonValueKind.String => StringUtils.EscapeString(element.GetString() ?? ""),
            _ => throw new ArgumentException($"Cannot format {element.ValueKind} as primitive")
        };
    }

    /// <summary>
    /// Formats a number JsonElement as a string.
    /// </summary>
    private static string FormatNumber(JsonElement element)
    {
        if (element.TryGetInt32(out int intValue))
        {
            return intValue.ToString(CultureInfo.InvariantCulture);
        }
        
        if (element.TryGetInt64(out long longValue))
        {
            return longValue.ToString(CultureInfo.InvariantCulture);
        }

        if (element.TryGetDouble(out double doubleValue))
        {
            return doubleValue.ToString("G17", CultureInfo.InvariantCulture);
        }

        return element.GetRawText();
    }

    /// <summary>
    /// Formats and joins multiple primitive values with a delimiter.
    /// </summary>
    /// <param name="elements">The primitive elements to format and join.</param>
    /// <param name="delimiter">The delimiter to use.</param>
    /// <returns>The joined string.</returns>
    public static string FormatAndJoinPrimitives(JsonElement[] elements, char delimiter)
    {
        var formattedValues = elements.Select(e => FormatPrimitive(e, delimiter));
        return string.Join(delimiter.ToString(), formattedValues);
    }
}