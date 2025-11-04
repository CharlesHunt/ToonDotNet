using System.Globalization;
using System.Text.Json;
using ToonFormat.Shared;

namespace ToonFormat.Decode;

/// <summary>
/// Parser for handling TOON syntax and converting text to structured data.
/// </summary>
internal static class ToonParser
{
    /// <summary>
    /// Parses an array header line.
    /// </summary>
    /// <param name="content">The line content to parse.</param>
    /// <param name="defaultDelimiter">The default delimiter to use.</param>
    /// <returns>Parsed header information and inline values, or null if not a valid header.</returns>
    public static ArrayHeaderParseResult? ParseArrayHeaderLine(string content, char defaultDelimiter)
    {
        string trimmed = content.TrimStart();

        // Find the bracket segment, accounting for quoted keys that may contain brackets
        int bracketStart = -1;

        // For quoted keys, find bracket after closing quote (not inside the quoted string)
        if (trimmed.StartsWith(Constants.DoubleQuote))
        {
            int closingQuoteIndex = StringUtils.FindClosingQuote(trimmed, 0);
            if (closingQuoteIndex == -1)
                return null;

            string afterQuote = trimmed[(closingQuoteIndex + 1)..];
            if (!afterQuote.StartsWith(Constants.OpenBracket))
                return null;

            // Calculate position in original content and find bracket after the quoted key
            int leadingWhitespace = content.Length - trimmed.Length;
            int keyEndIndex = leadingWhitespace + closingQuoteIndex + 1;
            bracketStart = content.IndexOf(Constants.OpenBracket, keyEndIndex);
        }
        else
        {
            // Unquoted key - find first bracket
            bracketStart = content.IndexOf(Constants.OpenBracket);
        }

        if (bracketStart == -1)
            return null;

        int bracketEnd = content.IndexOf(Constants.CloseBracket, bracketStart);
        if (bracketEnd == -1)
            return null;

        // Find the colon that comes after all brackets and braces
        int colonIndex = bracketEnd + 1;
        int braceEnd = colonIndex;

        // Check for fields segment (braces come after bracket)
        int braceStart = content.IndexOf(Constants.OpenBrace, bracketEnd);
        if (braceStart != -1 && braceStart < content.IndexOf(Constants.Colon, bracketEnd))
        {
            int foundBraceEnd = content.IndexOf(Constants.CloseBrace, braceStart);
            if (foundBraceEnd != -1)
            {
                braceEnd = foundBraceEnd + 1;
            }
        }

        // Now find colon after brackets and braces
        colonIndex = content.IndexOf(Constants.Colon, Math.Max(bracketEnd, braceEnd));
        if (colonIndex == -1)
            return null;

        // Extract and parse the key (might be quoted)
        string? key = null;
        if (bracketStart > 0)
        {
            string rawKey = content[..bracketStart].Trim();
            key = rawKey.StartsWith(Constants.DoubleQuote) ? ParseStringLiteral(rawKey) : rawKey;
        }

        string afterColon = content[(colonIndex + 1)..].Trim();
        string bracketContent = content[(bracketStart + 1)..bracketEnd];

        // Try to parse bracket segment
        BracketParseResult parsedBracket;
        try
        {
            parsedBracket = ParseBracketSegment(bracketContent, defaultDelimiter);
        }
        catch
        {
            return null;
        }

        // Check for fields segment
        string[]? fields = null;
        if (braceStart != -1 && braceStart < colonIndex)
        {
            int foundBraceEnd = content.IndexOf(Constants.CloseBrace, braceStart);
            if (foundBraceEnd != -1 && foundBraceEnd < colonIndex)
            {
                string fieldsContent = content[(braceStart + 1)..foundBraceEnd];
                var fieldValues = ParseDelimitedValues(fieldsContent, parsedBracket.Delimiter);
                fields = fieldValues.Select(field => ParseStringLiteral(field.Trim())).ToArray();
            }
        }

        var header = new ArrayHeaderInfo
        {
            Key = key,
            Length = parsedBracket.Length,
            Delimiter = parsedBracket.Delimiter,
            Fields = fields,
            HasLengthMarker = parsedBracket.HasLengthMarker
        };

        JsonElement[]? inlineValues = null;
        if (!string.IsNullOrWhiteSpace(afterColon))
        {
            var valueStrings = ParseDelimitedValues(afterColon, parsedBracket.Delimiter);
            inlineValues = MapRowValuesToPrimitives(valueStrings);
        }

        return new ArrayHeaderParseResult
        {
            Header = header,
            InlineValues = inlineValues
        };
    }

    /// <summary>
    /// Result of parsing bracket segment.
    /// </summary>
    private class BracketParseResult
    {
        public required int Length { get; set; }
        public required char Delimiter { get; set; }
        public required bool HasLengthMarker { get; set; }
    }

    /// <summary>
    /// Parses a bracket segment to extract length, delimiter, and length marker information.
    /// </summary>
    private static BracketParseResult ParseBracketSegment(string seg, char defaultDelimiter)
    {
        bool hasLengthMarker = false;
        string content = seg;

        // Check for length marker
        if (content.StartsWith(Constants.Hash))
        {
            hasLengthMarker = true;
            content = content[1..];
        }

        // Check for delimiter suffix
        char delimiter = defaultDelimiter;
        if (content.EndsWith(Constants.Tab))
        {
            delimiter = Constants.Delimiters.Tab;
            content = content[..^1];
        }
        else if (content.EndsWith(Constants.Pipe))
        {
            delimiter = Constants.Delimiters.Pipe;
            content = content[..^1];
        }

        if (!int.TryParse(content, out int length))
        {
            throw new InvalidOperationException($"Invalid array length: {seg}");
        }

        return new BracketParseResult
        {
            Length = length,
            Delimiter = delimiter,
            HasLengthMarker = hasLengthMarker
        };
    }

    /// <summary>
    /// Parses delimited values, respecting quoted strings.
    /// </summary>
    public static string[] ParseDelimitedValues(string input, char delimiter)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
        int i = 0;

        while (i < input.Length)
        {
            char c = input[i];

            if (c == Constants.Backslash && i + 1 < input.Length && inQuotes)
            {
                // Escape sequence in quoted string
                current.Append(c);
                current.Append(input[i + 1]);
                i += 2;
                continue;
            }

            if (c == Constants.DoubleQuote)
            {
                inQuotes = !inQuotes;
                current.Append(c);
                i++;
                continue;
            }

            if (c == delimiter && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
                i++;
                continue;
            }

            current.Append(c);
            i++;
        }

        // Add last value
        if (current.Length > 0 || values.Count > 0)
        {
            values.Add(current.ToString().Trim());
        }

        return values.ToArray();
    }

    /// <summary>
    /// Maps string values to JsonElement primitives.
    /// </summary>
    public static JsonElement[] MapRowValuesToPrimitives(string[] values)
    {
        return values.Select(v => LiteralUtils.ParsePrimitiveToken(v)).ToArray();
    }

    /// <summary>
    /// Parses a string literal, handling quotes and escaping.
    /// </summary>
    public static string ParseStringLiteral(string token)
    {
        string trimmed = token.Trim();

        if (trimmed.StartsWith(Constants.DoubleQuote))
        {
            // Find the closing quote, accounting for escaped quotes
            int closingQuoteIndex = StringUtils.FindClosingQuote(trimmed, 0);

            if (closingQuoteIndex == -1)
            {
                throw new InvalidOperationException("Unterminated string: missing closing quote");
            }

            if (closingQuoteIndex != trimmed.Length - 1)
            {
                throw new InvalidOperationException("Unexpected characters after closing quote");
            }

            string content = trimmed[1..closingQuoteIndex];
            return StringUtils.UnescapeString($"\"{content}\"");
        }

        return trimmed;
    }

    /// <summary>
    /// Parses a key token from content.
    /// </summary>
    public static KeyParseResult ParseKeyToken(string content, int start)
    {
        if (content[start] == Constants.DoubleQuote)
        {
            return ParseQuotedKey(content, start);
        }
        else
        {
            return ParseUnquotedKey(content, start);
        }
    }

    /// <summary>
    /// Result of parsing a key.
    /// </summary>
    public class KeyParseResult
    {
        public required string Key { get; set; }
        public required int End { get; set; }
    }

    /// <summary>
    /// Parses an unquoted key.
    /// </summary>
    private static KeyParseResult ParseUnquotedKey(string content, int start)
    {
        int end = start;
        while (end < content.Length && content[end] != Constants.Colon)
        {
            end++;
        }

        // Validate that a colon was found
        if (end >= content.Length || content[end] != Constants.Colon)
        {
            throw new InvalidOperationException("Missing colon after key");
        }

        string key = content[start..end].Trim();

        // Skip the colon
        end++;

        return new KeyParseResult { Key = key, End = end };
    }

    /// <summary>
    /// Parses a quoted key.
    /// </summary>
    private static KeyParseResult ParseQuotedKey(string content, int start)
    {
        // Find the closing quote, accounting for escaped quotes
        int closingQuoteIndex = StringUtils.FindClosingQuote(content, start);

        if (closingQuoteIndex == -1)
        {
            throw new InvalidOperationException("Unterminated quoted key");
        }

        // Extract and unescape the key content
        string keyContent = content[(start + 1)..closingQuoteIndex];
        string key = StringUtils.UnescapeString($"\"{keyContent}\"");
        int end = closingQuoteIndex + 1;

        // Validate and skip colon after quoted key
        if (end >= content.Length || content[end] != Constants.Colon)
        {
            throw new InvalidOperationException("Missing colon after key");
        }
        end++;

        return new KeyParseResult { Key = key, End = end };
    }

    /// <summary>
    /// Checks if content represents an array header after a hyphen.
    /// </summary>
    public static bool IsArrayHeaderAfterHyphen(string content)
    {
        return content.Trim().StartsWith(Constants.OpenBracket) && FindUnquotedChar(content, Constants.Colon) != -1;
    }

    /// <summary>
    /// Checks if content represents an object first field after a hyphen.
    /// </summary>
    public static bool IsObjectFirstFieldAfterHyphen(string content)
    {
        return FindUnquotedChar(content, Constants.Colon) != -1;
    }

    /// <summary>
    /// Finds an unquoted character in the content.
    /// </summary>
    private static int FindUnquotedChar(string content, char target)
    {
        bool inQuotes = false;
        
        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            
            if (c == Constants.DoubleQuote)
            {
                inQuotes = !inQuotes;
            }
            else if (c == target && !inQuotes)
            {
                return i;
            }
            else if (c == Constants.Backslash && inQuotes && i + 1 < content.Length)
            {
                i++; // Skip escaped character
            }
        }
        
        return -1;
    }
}