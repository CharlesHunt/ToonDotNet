using System.Text.Json;
using ToonFormat.Shared;

namespace ToonFormat.Decode;

/// <summary>
/// Main decoder class for converting TOON format strings to JsonElement values.
/// </summary>
internal static class ToonDecoder
{
    /// <summary>
    /// Decodes a TOON format string to a JsonElement.
    /// </summary>
    /// <param name="input">The TOON format string to decode.</param>
    /// <param name="options">Decoding options.</param>
    /// <returns>The decoded JsonElement.</returns>
    public static JsonElement DecodeValue(string input, DecodeOptions options)
    {
        var scanResult = ToonScanner.ToParsedLines(input, options.Indent, options.Strict);

        if (scanResult.Lines.Length == 0)
        {
            throw new InvalidOperationException("Cannot decode empty input: input must be a non-empty string");
        }

        var cursor = new LineCursor(scanResult.Lines, scanResult.BlankLines);
        return DecodeValueFromLines(cursor, options);
    }

    /// <summary>
    /// Decodes a value from parsed lines.
    /// </summary>
    private static JsonElement DecodeValueFromLines(LineCursor cursor, DecodeOptions options)
    {
        var first = cursor.Peek();
        if (first == null)
        {
            throw new InvalidOperationException("No content to decode");
        }

        // Check for root array
        if (ToonParser.IsArrayHeaderAfterHyphen(first.Content))
        {
            var headerInfo = ToonParser.ParseArrayHeaderLine(first.Content, Constants.DefaultDelimiter);
            if (headerInfo != null)
            {
                cursor.Advance(); // Move past the header line
                return DecodeArrayFromHeader(headerInfo.Header, headerInfo.InlineValues, cursor, 0, options);
            }
        }

        // Check for single primitive value
        if (cursor.Length == 1 && !IsKeyValueLine(first))
        {
            return LiteralUtils.ParsePrimitiveToken(first.Content.Trim());
        }

        // Default to object
        return DecodeObject(cursor, 0, options);
    }

    /// <summary>
    /// Checks if a line represents a key-value pair.
    /// </summary>
    private static bool IsKeyValueLine(ParsedLine line)
    {
        string content = line.Content;
        
        // Look for unquoted colon or quoted key followed by colon
#if NETSTANDARD2_0
        if (content.StartsWith(Constants.DoubleQuote.ToString()))
#else
        if (content.StartsWith(Constants.DoubleQuote))
#endif
        {
            // Quoted key - find the closing quote
            int closingQuoteIndex = StringUtils.FindClosingQuote(content, 0);
            if (closingQuoteIndex == -1)
                return false;
            
            // Check if colon exists after quoted key (may have array/brace syntax between)
#if NETSTANDARD2_0
            return content.Substring(closingQuoteIndex + 1).Contains(Constants.Colon.ToString());
#else
            return content[(closingQuoteIndex + 1)..].Contains(Constants.Colon);
#endif
        }
        else
        {
            // Unquoted key - look for first colon not inside quotes
            return content.Contains(Constants.Colon);
        }
    }

    /// <summary>
    /// Decodes an object from the cursor.
    /// </summary>
    private static JsonElement DecodeObject(LineCursor cursor, int baseDepth, DecodeOptions options)
    {
        var properties = new Dictionary<string, JsonElement>();

        // Detect the actual depth of the first field (may differ from baseDepth in nested structures)
        int? computedDepth = null;

        while (!cursor.AtEnd)
        {
            var line = cursor.Peek();
            if (line == null || line.Depth < baseDepth)
                break;

            if (computedDepth == null && line.Depth >= baseDepth)
            {
                computedDepth = line.Depth;
            }

            if (line.Depth == computedDepth)
            {
                var (key, value) = DecodeKeyValuePair(line, cursor, computedDepth.Value, options);
                properties[key] = value;
            }
            else
            {
                // Different depth (shallower or deeper) - stop object parsing
                break;
            }
        }

        return JsonDocument.Parse(JsonSerializer.Serialize(properties)).RootElement;
    }

    /// <summary>
    /// Decodes a key-value pair from a line.
    /// </summary>
    private static (string key, JsonElement value) DecodeKeyValuePair(ParsedLine line, LineCursor cursor, int baseDepth, DecodeOptions options)
    {
        cursor.Advance(); // Consume the line

        var result = DecodeKeyValue(line.Content, cursor, baseDepth, options);
        return (result.Key, result.Value);
    }

    /// <summary>
    /// Decodes key-value content.
    /// </summary>
    private static (string Key, JsonElement Value, int FollowDepth) DecodeKeyValue(string content, LineCursor cursor, int baseDepth, DecodeOptions options)
    {
        // Check for array header first (before parsing key)
        var arrayHeader = ToonParser.ParseArrayHeaderLine(content, Constants.DefaultDelimiter);
        if (arrayHeader != null && arrayHeader.Header.Key != null)
        {
            var value = DecodeArrayFromHeader(arrayHeader.Header, arrayHeader.InlineValues, cursor, baseDepth, options);
            // After an array, subsequent fields are at baseDepth + 1 (where array content is)
            return (arrayHeader.Header.Key, value, baseDepth + 1);
        }

        // Parse regular key-value
        var keyResult = ToonParser.ParseKeyToken(content, 0);
    string valueContent;
#if NETSTANDARD2_0
    valueContent = content.Substring(keyResult.End).Trim();
#else
    valueContent = content[keyResult.End..].Trim();
#endif

        JsonElement parsedValue;
        int followDepth = baseDepth;

        if (string.IsNullOrWhiteSpace(valueContent))
        {
            // Value on next line(s) or empty
            if (cursor.HasMoreAtDepth(baseDepth + 1))
            {
                // Nested content
                parsedValue = DecodeObject(cursor, baseDepth + 1, options);
                followDepth = baseDepth + 1;
            }
            else
            {
                // Empty object
                parsedValue = JsonDocument.Parse("{}").RootElement;
            }
        }
        else
        {
            // Inline primitive value
            parsedValue = LiteralUtils.ParsePrimitiveToken(valueContent);
        }

        return (keyResult.Key, parsedValue, followDepth);
    }

    /// <summary>
    /// Decodes an array from header information.
    /// </summary>
    private static JsonElement DecodeArrayFromHeader(ArrayHeaderInfo header, JsonElement[]? inlineValues, LineCursor cursor, int baseDepth, DecodeOptions options)
    {
        if (options.Strict)
        {
            ValidationUtils.ValidateNoBlankLinesInRange(cursor.BlankLines.ToList(), cursor.Current()?.LineNumber ?? 1, cursor.Peek()?.LineNumber ?? int.MaxValue);
        }

        // Handle inline values
        if (inlineValues != null)
        {
            if (options.Strict)
            {
                ValidationUtils.AssertExpectedCount(header.Length, inlineValues.Length, "inline values");
            }
            
            return JsonDocument.Parse(JsonSerializer.Serialize(inlineValues)).RootElement;
        }

        // Handle empty arrays
        if (header.Length == 0)
        {
            return JsonDocument.Parse("[]").RootElement;
        }

        // Handle tabular arrays (with fields)
        if (header.Fields != null)
        {
            return DecodeTabularArray(header, cursor, baseDepth, options);
        }

        // Handle list arrays
        return DecodeListArray(header, cursor, baseDepth, options);
    }

    /// <summary>
    /// Decodes a tabular array.
    /// </summary>
    private static JsonElement DecodeTabularArray(ArrayHeaderInfo header, LineCursor cursor, int baseDepth, DecodeOptions options)
    {
        var rows = new List<Dictionary<string, JsonElement>>();
        int expectedDepth = baseDepth + 1;

        for (int i = 0; i < header.Length; i++)
        {
            var line = cursor.PeekAtDepth(expectedDepth);
            if (line == null)
            {
                if (options.Strict)
                {
                    ValidationUtils.AssertExpectedCount(header.Length, i, "tabular rows");
                }
                break;
            }

            cursor.Advance();
            
            var values = ToonParser.ParseDelimitedValues(line.Content, header.Delimiter);
            var primitives = ToonParser.MapRowValuesToPrimitives(values);

            var row = new Dictionary<string, JsonElement>();
            for (int j = 0; j < header.Fields!.Length && j < primitives.Length; j++)
            {
                row[header.Fields[j]] = primitives[j];
            }

            rows.Add(row);
        }

        if (options.Strict)
        {
            ValidationUtils.ValidateNoExtraTabularRows(header.Length, rows.Count);
        }

        return JsonDocument.Parse(JsonSerializer.Serialize(rows)).RootElement;
    }

    /// <summary>
    /// Decodes a list array.
    /// </summary>
    private static JsonElement DecodeListArray(ArrayHeaderInfo header, LineCursor cursor, int baseDepth, DecodeOptions options)
    {
        var items = new List<JsonElement>();
        int expectedDepth = baseDepth + 1;

        for (int i = 0; i < header.Length; i++)
        {
            var line = cursor.PeekAtDepth(expectedDepth);
            if (line == null)
            {
                if (options.Strict)
                {
                    ValidationUtils.AssertExpectedCount(header.Length, i, "list items");
                }
                break;
            }

            // Check if line starts with list marker
            if (line.Content.StartsWith(Constants.ListItemPrefix))
            {
                cursor.Advance();
#if NETSTANDARD2_0
                string itemContent = line.Content.Substring(Constants.ListItemPrefix.Length);
#else
                string itemContent = line.Content[Constants.ListItemPrefix.Length..];
#endif
                
                // Check for nested object or array
                if (ToonParser.IsArrayHeaderAfterHyphen(itemContent))
                {
                    // Use the parent array's delimiter as the default for nested arrays
                    var nestedHeader = ToonParser.ParseArrayHeaderLine(itemContent, header.Delimiter);
                    if (nestedHeader != null)
                    {
                        var nestedArray = DecodeArrayFromHeader(nestedHeader.Header, nestedHeader.InlineValues, cursor, expectedDepth, options);
                        items.Add(nestedArray);
                        continue;
                    }
                }
                
                if (ToonParser.IsObjectFirstFieldAfterHyphen(itemContent))
                {
                    // Object starting on this line
                    var result = DecodeKeyValue(itemContent, cursor, expectedDepth, options);
                    var objDict = new Dictionary<string, JsonElement> { [result.Key] = result.Value };
                    
                    // Check for more properties at the same depth
                    while (cursor.HasMoreAtDepth(expectedDepth + 1))
                    {
                        var nextLine = cursor.PeekAtDepth(expectedDepth + 1);
                        if (nextLine == null) break;
                        
                        var (nextKey, nextValue) = DecodeKeyValuePair(nextLine, cursor, expectedDepth + 1, options);
                        objDict[nextKey] = nextValue;
                    }
                    
                    items.Add(JsonDocument.Parse(JsonSerializer.Serialize(objDict)).RootElement);
                }
                else
                {
                    // Primitive value
                    items.Add(LiteralUtils.ParsePrimitiveToken(itemContent));
                }
            }
            else
            {
                // Non-list item format - treat as primitive
                cursor.Advance();
                items.Add(LiteralUtils.ParsePrimitiveToken(line.Content));
            }
        }

        if (options.Strict)
        {
            ValidationUtils.ValidateNoExtraListItems(header.Length, items.Count);
        }

        return JsonDocument.Parse(JsonSerializer.Serialize(items)).RootElement;
    }
}