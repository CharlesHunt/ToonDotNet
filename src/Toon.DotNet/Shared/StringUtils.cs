using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ToonFormat.Shared;

/// <summary>
/// Utility methods for string operations in TOON format processing.
/// </summary>
internal static class StringUtils
{
    /// <summary>
    /// Trims a token extracted during decoding: a key token before a
    /// key-value colon or an entry key's colon (§7.4, §9.5), a value
    /// token after a key-value or array-header colon, or a token around
    /// each delimiter-separated cell. Spec §12 (v4.0.0, promoted to MUST)
    /// restricts this trimming to exactly U+0020 — tabs, non-breaking
    /// space, and every other whitespace category remain part of the
    /// token. This is a genuine semantic conflict with the library's
    /// pre-v4 behavior (plain <see cref="string.Trim()"/>, which strips
    /// the broader <see cref="char.IsWhiteSpace(char)"/> set): the same
    /// bytes can decode to a different string depending on which rule
    /// applies. <paramref name="legacyCompatibility"/> (from
    /// <see cref="DecodeOptions.LegacyCompatibility"/>) opts back into
    /// the old broader-whitespace behavior; the default (v4-correct)
    /// behavior trims only U+0020.
    /// </summary>
    public static string TrimToken(string value, bool legacyCompatibility)
    {
        if (legacyCompatibility)
        {
            return value.Trim();
        }

        int start = 0;
        while (start < value.Length && value[start] == Constants.Space)
        {
            start++;
        }

        int end = value.Length;
        while (end > start && value[end - 1] == Constants.Space)
        {
            end--;
        }

#if NETSTANDARD2_0
        return value.Substring(start, end - start);
#else
        return value[start..end];
#endif
    }

    /// <summary>
    /// Finds the closing quote for a quoted string, handling escape sequences.
    /// </summary>
    /// <param name="input">The input string to search.</param>
    /// <param name="startIndex">The index of the opening quote.</param>
    /// <returns>The index of the closing quote, or -1 if not found.</returns>
    public static int FindClosingQuote(string input, int startIndex)
    {
        if (startIndex >= input.Length || input[startIndex] != Constants.DoubleQuote)
            return -1;

        for (int i = startIndex + 1; i < input.Length; i++)
        {
            char c = input[i];
            
            if (c == Constants.DoubleQuote)
            {
                return i;
            }
            
            if (c == Constants.Backslash && i + 1 < input.Length)
            {
                // Skip the escaped character
                i++;
            }
        }

        return -1;
    }

    /// <summary>
    /// Escapes a string for use in TOON format.
    /// </summary>
    /// <param name="value">The string to escape.</param>
    /// <param name="delimiter">The active delimiter in scope for this value (spec §11.1) — a value is quoted for containing this delimiter, not for containing an inactive one.</param>
    /// <param name="specVersion">Which TOON grammar's quoting rules to apply. <see cref="ToonSpecVersion.V3"/> uses the v3.3.2 numeric-like pattern (leading '-' only); <see cref="ToonSpecVersion.V4"/> uses the v4.0.0 pattern (leading '+' also forces quoting).</param>
    /// <returns>The escaped string with quotes if necessary.</returns>
    public static string EscapeString(string value, char delimiter = Constants.DefaultDelimiter, ToonSpecVersion specVersion = ToonSpecVersion.V4)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        if (!ShouldQuoteString(value, delimiter, specVersion))
            return value;

        return QuoteAndEscape(value);
    }

    /// <summary>
    /// Escapes a key for use in TOON format, per spec section 7.3's
    /// unquoted-key identifier pattern rather than the value-quoting rule
    /// set (section 7.2) used by EscapeString.
    /// </summary>
    /// <param name="key">The key to escape.</param>
    /// <returns>The key as-is if it matches the unquoted-key pattern, otherwise a quoted and escaped form.</returns>
    public static string EscapeKey(string key)
    {
        if (IsValidUnquotedKey(key))
            return key;

        return QuoteAndEscape(key);
    }

    /// <summary>
    /// Checks whether a key matches the spec's unquoted-key pattern:
    /// ^[A-Za-z_][A-Za-z0-9_.]*$
    /// </summary>
    private static bool IsValidUnquotedKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        char first = key[0];
        if (!(IsAsciiLetter(first) || first == '_'))
            return false;

        for (int i = 1; i < key.Length; i++)
        {
            char c = key[i];
            if (!(IsAsciiLetter(c) || IsAsciiDigit(c) || c == '_' || c == '.'))
                return false;
        }

        return true;
    }

    private static bool IsAsciiLetter(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

    private static bool IsAsciiDigit(char c) => c >= '0' && c <= '9';

    /// <summary>
    /// Wraps a value in double quotes and escapes it per spec section 7.1.
    /// Shared by EscapeString and EscapeKey - both use the same escape
    /// table, only the "does this need quoting at all" decision differs
    /// between values and keys.
    /// </summary>
    private static string QuoteAndEscape(string value)
    {
        var sb = new StringBuilder();
        sb.Append(Constants.DoubleQuote);

        foreach (char c in value)
        {
            switch (c)
            {
                case Constants.DoubleQuote:
                    sb.Append("\\\"");
                    break;
                case Constants.Backslash:
                    sb.Append("\\\\");
                    break;
                case Constants.Newline:
                    sb.Append("\\n");
                    break;
                case Constants.CarriageReturn:
                    sb.Append("\\r");
                    break;
                case Constants.Tab:
                    sb.Append("\\t");
                    break;
                default:
                    // Spec §7.1: other C0 controls (U+0000-001F besides the
                    // named escapes above) MUST be emitted as \uXXXX, not
                    // as raw bytes (also a §15 security concern).
                    if (c <= '\u001F')
                    {
                        sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }

        sb.Append(Constants.DoubleQuote);
        return sb.ToString();
    }

    /// <summary>
    /// Unescapes a quoted string from TOON format.
    /// </summary>
    /// <param name="quotedValue">The quoted string to unescape.</param>
    /// <returns>The unescaped string.</returns>
    public static string UnescapeString(string quotedValue)
    {
    if (quotedValue.Length < 2 || quotedValue[0] != Constants.DoubleQuote ||
#if NETSTANDARD2_0
        quotedValue[quotedValue.Length - 1] != Constants.DoubleQuote)
        return quotedValue;
#else
        quotedValue[^1] != Constants.DoubleQuote)
        return quotedValue;
#endif

    var sb = new StringBuilder();
#if NETSTANDARD2_0
    string content = quotedValue.Substring(1, quotedValue.Length - 2); // Remove surrounding quotes
#else
    string content = quotedValue[1..^1]; // Remove surrounding quotes
#endif

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            
            if (c == Constants.Backslash && i + 1 < content.Length)
            {
                char nextChar = content[i + 1];
                switch (nextChar)
                {
                    case Constants.DoubleQuote:
                        sb.Append(Constants.DoubleQuote);
                        i++; // Skip the escaped character
                        break;
                    case Constants.Backslash:
                        sb.Append(Constants.Backslash);
                        i++; // Skip the escaped character
                        break;
                    case 'n':
                        sb.Append(Constants.Newline);
                        i++; // Skip the escaped character
                        break;
                    case 'r':
                        sb.Append(Constants.CarriageReturn);
                        i++; // Skip the escaped character
                        break;
                    case 't':
                        sb.Append(Constants.Tab);
                        i++; // Skip the escaped character
                        break;
                    case 'u':
                        // Spec §7.1: exactly 4 hex digits, case-insensitive;
                        // surrogate code points (U+D800-DFFF) MUST be
                        // rejected.
                        if (i + 5 >= content.Length)
                        {
                            throw new InvalidOperationException($"Invalid \\u escape: not enough hex digits in \"{quotedValue}\"");
                        }

#if NETSTANDARD2_0
                        string hex = content.Substring(i + 2, 4);
#else
                        string hex = content[(i + 2)..(i + 6)];
#endif
                        if (!ushort.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort codeUnit))
                        {
                            throw new InvalidOperationException($"Invalid \\u escape: \"{hex}\" is not valid hex in \"{quotedValue}\"");
                        }

                        if (codeUnit >= 0xD800 && codeUnit <= 0xDFFF)
                        {
                            throw new InvalidOperationException($"Invalid \\u escape: surrogate code point \\u{hex} is not allowed in \"{quotedValue}\"");
                        }

                        sb.Append((char)codeUnit);
                        i += 5; // Skip 'u' plus the 4 hex digits
                        break;
                    default:
                        // Spec §7.1: decoder MUST reject any escape
                        // sequence not in the table above.
                        throw new InvalidOperationException($"Invalid escape sequence \"\\{nextChar}\" in \"{quotedValue}\"");
                }
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Determines if a string should be quoted in TOON format.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="delimiter">The active delimiter in scope (spec §11.1) — only this delimiter forces quoting; a string containing an inactive delimiter character doesn't need quoting on that basis alone.</param>
    /// <param name="specVersion">Which numeric-like pattern to apply — see <see cref="EscapeString"/>.</param>
    private static bool ShouldQuoteString(string value, char delimiter, ToonSpecVersion specVersion)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        // Check for reserved literals
        if (value == Constants.NullLiteral || value == Constants.TrueLiteral || value == Constants.FalseLiteral)
            return true;

        // Spec §7.2: leading/trailing whitespace MUST be quoted.
        if (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1]))
            return true;

        // Spec §7.2: a value equal to or starting with "-" MUST be quoted
        // (ambiguous with the list-item marker syntax).
        if (value[0] == '-')
            return true;

        // Check for special characters that require quoting. Tab is a
        // control character (caught by IsControl below) regardless of
        // whether it's also the active delimiter, so it doesn't need a
        // separate check here.
        foreach (char c in value)
        {
            if (char.IsControl(c) || c == Constants.DoubleQuote || c == Constants.Backslash ||
                c == delimiter ||
                c == Constants.Colon || c == Constants.OpenBracket || c == Constants.CloseBracket ||
                c == Constants.OpenBrace || c == Constants.CloseBrace || c == Constants.Hash)
            {
                return true;
            }
        }

        // Spec §7.2: numeric-like pattern — exact regex, not
        // double.TryParse, which both over-quotes strings TryParse merely
        // tolerates (e.g. thousands separators) and under-quotes all-digit
        // strings whose magnitude exceeds double's range.
        //
        // The sign class differs by spec version: v3.3.2 is
        // /^-?[0-9]+(?:\.[0-9]+)?(?:e[+-]?[0-9]+)?$/i (leading '-' only);
        // v4.0.0 widens it to [+-]? — a leading '+' also forces quoting.
        // Emitting genuine v3.3.2 output means reproducing its narrower
        // pattern too, even though it's a known gap: without the v4
        // widening, an unquoted "+5" string value silently decodes back
        // as the number 5 on any decoder (.NET's NumberStyles.Integer/
        // Float both accept a leading '+' via AllowLeadingSign) — a real
        // round-trip bug, but one that is part of what "emit v3.3.2"
        // means when the caller explicitly asks for it.
        var pattern = specVersion == ToonSpecVersion.V3 ? NumericLikePatternV3 : NumericLikePatternV4;
        if (pattern.IsMatch(value))
            return true;

        return false;
    }

    private static readonly Regex NumericLikePatternV3 = new Regex(
        @"^-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?$",
        RegexOptions.CultureInvariant);

    private static readonly Regex NumericLikePatternV4 = new Regex(
        @"^[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?$",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Checks if a string is quoted.
    /// </summary>
    public static bool IsQuoted(string value)
    {
    if (value.Length < 2) return false;
#if NETSTANDARD2_0
    return value[0] == Constants.DoubleQuote && value[value.Length - 1] == Constants.DoubleQuote;
#else
    return value[0] == Constants.DoubleQuote && value[^1] == Constants.DoubleQuote;
#endif
    }
}