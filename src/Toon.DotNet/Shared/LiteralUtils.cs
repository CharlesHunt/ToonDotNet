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
        if (token == null)
            return JsonDocument.Parse("null").RootElement;

        // Spec §9.1: an empty token (e.g. between two delimiters) decodes to
        // an empty string, not null.
        if (token.Length == 0)
            return JsonDocument.Parse("\"\"").RootElement;

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
            // Spec §4/§9.1: "[]" (root or object-field value position) decodes
            // as an empty array. Unquoted strings can never legally contain
            // "[" or "]" (§7.2 requires quoting), so this is unambiguous.
            case "[]":
                return JsonDocument.Parse("[]").RootElement;
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
        // Spec §4: "05", "0001", "-05" are strings, not numbers (a leading
        // zero followed by another digit). "0.5", "0e1", "-0.5" (zero
        // followed by '.' or 'e'/'E') and a lone "0" remain valid numbers.
        if (HasForbiddenLeadingZero(value))
        {
            element = default;
            return false;
        }

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
    /// Checks whether a token has a leading zero followed by another digit
    /// (e.g. "05", "-0001"), which spec §4 excludes from the number grammar.
    /// </summary>
    private static bool HasForbiddenLeadingZero(string value)
    {
        int start = value.Length > 0 && value[0] == '-' ? 1 : 0;

        if (value.Length <= start + 1 || value[start] != '0')
            return false;

        char next = value[start + 1];
        return next >= '0' && next <= '9';
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
            JsonValueKind.String => StringUtils.EscapeString(element.GetString() ?? "", delimiter),
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
            return FormatCanonicalDouble(doubleValue);
        }

        return element.GetRawText();
    }

    private static readonly char[] ExponentMarkers = { 'e', 'E' };

    /// <summary>
    /// Formats a double per spec §2's canonical number form: no more
    /// precision than round-trip requires, -0 normalized to 0, and no
    /// exponent for 0 or 1e-6 &lt;= |n| &lt; 1e21.
    /// </summary>
    private static string FormatCanonicalDouble(double value)
    {
        if (value == 0)
            return "0"; // covers -0 -> 0 too (-0.0 == 0.0 in IEEE 754)

#if NETSTANDARD2_0
        // .NET Standard 2.0 targets include .NET Framework 4.6.1+, whose
        // default double.ToString() predates the shortest-round-trip
        // ("Ryu") algorithm and isn't guaranteed to round-trip — G17 is
        // the safe, if verbose, choice there.
        string formatted = value.ToString("G17", CultureInfo.InvariantCulture);
#else
        // Modern .NET's default ToString() already produces the shortest
        // string that round-trips to the exact same double.
        string formatted = value.ToString(CultureInfo.InvariantCulture);
#endif

        int eIndex = formatted.IndexOfAny(ExponentMarkers);
        if (eIndex < 0)
            return formatted;

        double magnitude = Math.Abs(value);
        bool exponentForbidden = magnitude >= 1e-6 && magnitude < 1e21;

        if (exponentForbidden)
            return ExpandExponentialNotation(formatted, eIndex);

        // Spec's own examples ("1e-7", "1e+21") use a lowercase marker.
        return formatted.Substring(0, eIndex) + "e" + formatted.Substring(eIndex + 1);
    }

    /// <summary>
    /// Converts a .NET exponential number string (e.g. "1.23E+19") to
    /// fixed-point notation, preserving the exact same significant digits
    /// and trimming trailing fractional zeros per spec §2.
    /// </summary>
    private static string ExpandExponentialNotation(string formatted, int eIndex)
    {
        string mantissaPart = formatted.Substring(0, eIndex);
        string exponentPart = formatted.Substring(eIndex + 1);
        int exponent = int.Parse(exponentPart, NumberStyles.Integer, CultureInfo.InvariantCulture);

        bool negative = mantissaPart.Length > 0 && mantissaPart[0] == '-';
        if (negative)
            mantissaPart = mantissaPart.Substring(1);

        int dotIndex = mantissaPart.IndexOf('.');
        string digits;
        int pointPosition; // count of significant digits before the decimal point
        if (dotIndex >= 0)
        {
            digits = mantissaPart.Substring(0, dotIndex) + mantissaPart.Substring(dotIndex + 1);
            pointPosition = dotIndex;
        }
        else
        {
            digits = mantissaPart;
            pointPosition = mantissaPart.Length;
        }

        int newPointPosition = pointPosition + exponent;

        string result;
        if (newPointPosition <= 0)
        {
            result = "0." + new string('0', -newPointPosition) + digits;
        }
        else if (newPointPosition >= digits.Length)
        {
            result = digits + new string('0', newPointPosition - digits.Length);
        }
        else
        {
            result = digits.Substring(0, newPointPosition) + "." + digits.Substring(newPointPosition);
        }

        if (result.IndexOf('.') >= 0)
        {
            result = result.TrimEnd('0');
            if (result.Length > 0 && result[result.Length - 1] == '.')
                result = result.Substring(0, result.Length - 1);
        }

        return negative ? "-" + result : result;
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