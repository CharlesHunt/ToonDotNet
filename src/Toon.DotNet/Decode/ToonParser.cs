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
    /// <param name="strict">When true (default), a malformed bracket segment throws. When false, spec §14.2 permits falling through to key-value parsing instead (returns null).</param>
    /// <returns>Parsed header information and inline values, or null if not a valid header.</returns>
    public static ArrayHeaderParseResult? ParseArrayHeaderLine(string content, char defaultDelimiter, bool strict = true)
    {
        string trimmed = content.TrimStart();

        // Find the bracket segment, accounting for quoted keys that may contain brackets
        int bracketStart = -1;

        // For quoted keys, find bracket after closing quote (not inside the quoted string)
#if NETSTANDARD2_0
        if (trimmed.StartsWith(Constants.DoubleQuote.ToString()))
#else
        if (trimmed.StartsWith(Constants.DoubleQuote))
#endif
        {
            int closingQuoteIndex = StringUtils.FindClosingQuote(trimmed, 0);
            if (closingQuoteIndex == -1)
                return null;

#if NETSTANDARD2_0
            string afterQuote = trimmed.Substring(closingQuoteIndex + 1);
            if (!afterQuote.StartsWith(Constants.OpenBracket.ToString()))
#else
            string afterQuote = trimmed[(closingQuoteIndex + 1)..];
            if (!afterQuote.StartsWith(Constants.OpenBracket))
#endif
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

        // Check for fields segment (braces come after bracket). Uses a
        // brace-depth-aware search rather than a plain IndexOf so that
        // nested field groups (v4.0.0 RFC #46, e.g.
        // "{id,customer{name,country},total}") don't cause the outer
        // field list's close brace to be mistaken for an inner group's.
        int braceStart = content.IndexOf(Constants.OpenBrace, bracketEnd);
        if (braceStart != -1 && braceStart < content.IndexOf(Constants.Colon, bracketEnd))
        {
            int foundBraceEnd = FindMatchingCloseBrace(content, braceStart);
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
#if NETSTANDARD2_0
            string rawKey = content.Substring(0, bracketStart).Trim();
            key = rawKey.StartsWith(Constants.DoubleQuote.ToString()) ? ParseStringLiteral(rawKey) : rawKey;
#else
            string rawKey = content[..bracketStart].Trim();
            key = rawKey.StartsWith(Constants.DoubleQuote) ? ParseStringLiteral(rawKey) : rawKey;
#endif
        }

#if NETSTANDARD2_0
    string afterColon = content.Substring(colonIndex + 1).Trim();
    string bracketContent = content.Substring(bracketStart + 1, bracketEnd - (bracketStart + 1));
#else
    string afterColon = content[(colonIndex + 1)..].Trim();
    string bracketContent = content[(bracketStart + 1)..bracketEnd];
#endif

        // Parse the bracket segment. By this point the line has an
        // unambiguous "...[...]...:" shape, so a malformed length (spec §6:
        // leading zeros, negative, non-numeric) is a genuine syntax error
        // in strict mode — propagate rather than silently falling through
        // to key-value parsing. Non-strict mode MAY fall through instead
        // (spec §14.2); it's a choice, not a requirement, so strict mode
        // keeps the stricter (and previously the only) behavior.
        BracketParseResult parsedBracket;
        if (strict)
        {
            parsedBracket = ParseBracketSegment(bracketContent, defaultDelimiter);
        }
        else
        {
            try
            {
                parsedBracket = ParseBracketSegment(bracketContent, defaultDelimiter);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        // Check for fields segment
        TabularField[]? fields = null;
        if (braceStart != -1 && braceStart < colonIndex)
        {
            int foundBraceEnd = FindMatchingCloseBrace(content, braceStart);
            if (foundBraceEnd != -1 && foundBraceEnd < colonIndex)
            {
#if NETSTANDARD2_0
                string fieldsContent = content.Substring(braceStart + 1, foundBraceEnd - (braceStart + 1));
#else
                string fieldsContent = content[(braceStart + 1)..foundBraceEnd];
#endif
                // Fields are always comma-delimited, regardless of the
                // data delimiter, and may contain nested field groups
                // (v4.0.0 RFC #46).
                fields = ParseFieldList(fieldsContent);
            }
        }

        // Spec §9.5: a keyed-tabular header's field list is REQUIRED. A
        // keyed-seg bracket with no fields segment is a malformed header
        // — same strict/non-strict fall-through convention as the
        // length-less bracket segment case above.
        if (parsedBracket.IsKeyedTabular && fields == null)
        {
            if (strict)
            {
                throw new InvalidOperationException($"Keyed tabular header requires a field list: {content}");
            }

            return null;
        }

        var header = new ArrayHeaderInfo
        {
            Key = key,
            Length = parsedBracket.Length,
            Delimiter = parsedBracket.Delimiter,
            Fields = fields,
            HasLengthMarker = parsedBracket.HasLengthMarker,
            IsKeyedTabular = parsedBracket.IsKeyedTabular
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
#if NETSTANDARD2_0
    public int Length { get; set; }
    public char Delimiter { get; set; }
    public bool HasLengthMarker { get; set; }
    public bool IsKeyedTabular { get; set; }
#else
    public required int Length { get; set; }
    public required char Delimiter { get; set; }
    public required bool HasLengthMarker { get; set; }
    public required bool IsKeyedTabular { get; set; }
#endif
    }

    /// <summary>
    /// Parses a bracket segment to extract length, delimiter, and length marker information.
    /// </summary>
    private static BracketParseResult ParseBracketSegment(string seg, char defaultDelimiter)
    {
        bool hasLengthMarker = false;
        string content = seg;

        // Check for length marker
#if NETSTANDARD2_0
        if (content.StartsWith(Constants.Hash.ToString()))
#else
        if (content.StartsWith(Constants.Hash))
#endif
        {
            hasLengthMarker = true;
            // Remove leading length marker char
#if NETSTANDARD2_0
            content = content.Substring(1);
#else
            content = content[1..];
#endif
        }

        // Check for delimiter suffix
        char delimiter = defaultDelimiter;
#if NETSTANDARD2_0
        if (content.EndsWith(Constants.Tab.ToString()))
#else
        if (content.EndsWith(Constants.Tab))
#endif
        {
            delimiter = Constants.Delimiters.Tab;
#if NETSTANDARD2_0
            content = content.Substring(0, content.Length - 1);
#else
            content = content[..^1];
#endif
        }
#if NETSTANDARD2_0
        else if (content.EndsWith(Constants.Pipe.ToString()))
#else
        else if (content.EndsWith(Constants.Pipe))
#endif
        {
            delimiter = Constants.Delimiters.Pipe;
#if NETSTANDARD2_0
            content = content.Substring(0, content.Length - 1);
#else
            content = content[..^1];
#endif
        }

        // Spec §12: decoders SHOULD tolerate surrounding whitespace around
        // tokens. Trimmed here, after the delimiter-suffix check above, so
        // a real tab/pipe delimiter marker (checked via EndsWith) is never
        // mistaken for trimmable padding.
        content = content.Trim();

        // Spec §6 keyed-seg grammar (v4.0.0 RFC #57, §9.5 keyed tabular
        // form for objects): "[" length ":" [ delimsym ] "]" — a literal
        // trailing colon inside the bracket segment (checked after the
        // delimiter suffix above, so "2:|" strips the pipe first, then
        // the colon) marks the keyed form rather than a plain array
        // header. A colon can never appear here otherwise, since a plain
        // array header's bracket content is digits (+ optional marker/
        // delimiter) only.
        bool isKeyedTabular = false;
#if NETSTANDARD2_0
        if (content.EndsWith(Constants.Colon.ToString()))
#else
        if (content.EndsWith(Constants.Colon))
#endif
        {
            isKeyedTabular = true;
#if NETSTANDARD2_0
            content = content.Substring(0, content.Length - 1);
#else
            content = content[..^1];
#endif
        }

        // Spec §6: length is a non-negative integer with no leading zeros;
        // a single "0" is the only canonical zero form.
        if (content.Length == 0 || (content.Length > 1 && content[0] == '0'))
        {
            throw new InvalidOperationException($"Invalid array length: {seg}");
        }

        // NumberStyles.None additionally rejects a leading sign, so "-1"
        // fails here too.
        if (!int.TryParse(content, NumberStyles.None, CultureInfo.InvariantCulture, out int length))
        {
            throw new InvalidOperationException($"Invalid array length: {seg}");
        }

        return new BracketParseResult
        {
            Length = length,
            Delimiter = delimiter,
            HasLengthMarker = hasLengthMarker,
            IsKeyedTabular = isKeyedTabular
        };
    }

    /// <summary>
    /// Finds the close brace matching the open brace at <paramref name="openBraceIndex"/>,
    /// tracking nesting depth (and ignoring braces inside quoted key
    /// segments) so a nested field group's own close brace isn't mistaken
    /// for the outer group's.
    /// </summary>
    /// <returns>The index of the matching close brace, or -1 if unbalanced.</returns>
    private static int FindMatchingCloseBrace(string content, int openBraceIndex)
    {
        int depth = 0;
        bool inQuotes = false;

        for (int i = openBraceIndex; i < content.Length; i++)
        {
            char c = content[i];

            if (c == Constants.DoubleQuote)
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (inQuotes)
                continue;

            if (c == Constants.OpenBrace)
            {
                depth++;
            }
            else if (c == Constants.CloseBrace)
            {
                depth--;
                if (depth == 0)
                    return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Parses a tabular header's field list (spec §9.3/§9.5), including
    /// any nested field groups (v4.0.0 RFC #46, e.g. "customer{name,country}").
    /// </summary>
    private static TabularField[] ParseFieldList(string content)
    {
        var segments = SplitTopLevelFieldSegments(content);
        var fields = new TabularField[segments.Length];
        for (int i = 0; i < segments.Length; i++)
        {
            fields[i] = ParseFieldSegment(segments[i]);
        }
        return fields;
    }

    /// <summary>
    /// Parses one field-list segment into a leaf field or a nested field
    /// group.
    /// </summary>
    private static TabularField ParseFieldSegment(string segment)
    {
        string trimmed = segment.Trim();
        int braceStart = FindUnquotedChar(trimmed, Constants.OpenBrace);

        if (braceStart == -1)
        {
            return new TabularField { Name = ParseStringLiteral(trimmed), Children = null };
        }

        if (trimmed.Length == 0 || trimmed[trimmed.Length - 1] != Constants.CloseBrace)
        {
            throw new InvalidOperationException($"Malformed nested field group: {segment}");
        }

#if NETSTANDARD2_0
        string namePart = trimmed.Substring(0, braceStart).Trim();
        string innerContent = trimmed.Substring(braceStart + 1, trimmed.Length - braceStart - 2);
#else
        string namePart = trimmed[..braceStart].Trim();
        string innerContent = trimmed[(braceStart + 1)..^1];
#endif

        var children = ParseFieldList(innerContent);
        if (children.Length == 0)
        {
            throw new InvalidOperationException($"Empty nested field group: {segment}");
        }

        return new TabularField { Name = ParseStringLiteral(namePart), Children = children };
    }

    /// <summary>
    /// Splits a field list on top-level commas, respecting quoted
    /// segments and nested brace groups (so a nested field group's own
    /// commas don't split it apart).
    /// </summary>
    private static string[] SplitTopLevelFieldSegments(string content)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
        int braceDepth = 0;
        int i = 0;

        while (i < content.Length)
        {
            char c = content[i];

            if (c == Constants.Backslash && i + 1 < content.Length && inQuotes)
            {
                current.Append(c);
                current.Append(content[i + 1]);
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

            if (!inQuotes && c == Constants.OpenBrace)
            {
                braceDepth++;
                current.Append(c);
                i++;
                continue;
            }

            if (!inQuotes && c == Constants.CloseBrace)
            {
                braceDepth--;
                current.Append(c);
                i++;
                continue;
            }

            if (!inQuotes && braceDepth == 0 && c == Constants.Comma)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
                i++;
                continue;
            }

            current.Append(c);
            i++;
        }

        if (current.Length > 0 || values.Count > 0)
        {
            values.Add(current.ToString().Trim());
        }

        return values.ToArray();
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

#if NETSTANDARD2_0
        if (trimmed.StartsWith(Constants.DoubleQuote.ToString()))
#else
        if (trimmed.StartsWith(Constants.DoubleQuote))
#endif
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

#if NETSTANDARD2_0
            string content = trimmed.Substring(1, closingQuoteIndex - 1);
#else
            string content = trimmed[1..closingQuoteIndex];
#endif
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
#if NETSTANDARD2_0
    public string Key { get; set; }
    public int End { get; set; }
#else
    public required string Key { get; set; }
    public required int End { get; set; }
#endif
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

#if NETSTANDARD2_0
    string key = content.Substring(start, end - start).Trim();
#else
    string key = content[start..end].Trim();
#endif

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
#if NETSTANDARD2_0
    string keyContent = content.Substring(start + 1, closingQuoteIndex - (start + 1));
#else
    string keyContent = content[(start + 1)..closingQuoteIndex];
#endif
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
#if NETSTANDARD2_0
        return content.Trim().StartsWith(Constants.OpenBracket.ToString()) && FindUnquotedChar(content, Constants.Colon) != -1;
#else
        return content.Trim().StartsWith(Constants.OpenBracket) && FindUnquotedChar(content, Constants.Colon) != -1;
#endif
    }

    /// <summary>
    /// Checks if content represents an object first field after a hyphen.
    /// </summary>
    public static bool IsObjectFirstFieldAfterHyphen(string content)
    {
        return FindUnquotedChar(content, Constants.Colon) != -1;
    }

    /// <summary>
    /// Checks whether content contains a colon outside of any quoted
    /// segment. Used for keyed-tabular entry-row line classification
    /// (spec §9.5: "every line at entry depth containing an unquoted
    /// colon is an entry row").
    /// </summary>
    public static bool HasUnquotedColon(string content)
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