using System.Text;

namespace ToonFormat.Shared;

/// <summary>
/// Utility methods for string operations in TOON format processing.
/// </summary>
internal static class StringUtils
{
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
    /// <returns>The escaped string with quotes if necessary.</returns>
    public static string EscapeString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        // Check if the string needs quoting
        bool needsQuoting = ShouldQuoteString(value);

        if (!needsQuoting)
            return value;

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
                    sb.Append(c);
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
        if (quotedValue.Length < 2 || quotedValue[0] != Constants.DoubleQuote || quotedValue[^1] != Constants.DoubleQuote)
            return quotedValue;

        var sb = new StringBuilder();
        string content = quotedValue[1..^1]; // Remove surrounding quotes

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
                    default:
                        sb.Append(c); // Keep the backslash if not a recognized escape
                        break;
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
    private static bool ShouldQuoteString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        // Check for reserved literals
        if (value == Constants.NullLiteral || value == Constants.TrueLiteral || value == Constants.FalseLiteral)
            return true;

        // Check for special characters that require quoting
        foreach (char c in value)
        {
            if (char.IsControl(c) || c == Constants.DoubleQuote || c == Constants.Backslash ||
                c == Constants.Comma || c == Constants.Pipe || c == Constants.Tab ||
                c == Constants.Colon || c == Constants.OpenBracket || c == Constants.CloseBracket ||
                c == Constants.OpenBrace || c == Constants.CloseBrace || c == Constants.Hash)
            {
                return true;
            }
        }

        // Check if it looks like a number
        if (double.TryParse(value, out _))
            return true;

        return false;
    }

    /// <summary>
    /// Checks if a string is quoted.
    /// </summary>
    public static bool IsQuoted(string value)
    {
        return value.Length >= 2 && value[0] == Constants.DoubleQuote && value[^1] == Constants.DoubleQuote;
    }
}