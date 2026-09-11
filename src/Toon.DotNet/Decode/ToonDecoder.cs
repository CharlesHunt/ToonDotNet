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
            // Spec §5: an empty document decodes to an empty object.
            return JsonDocument.Parse("{}").RootElement;
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

        // Check for root array (or root keyed-tabular object, spec §9.5,
        // which reuses the same "[...]{...}:" header shape but decodes
        // to an object rather than an array).
        if (ToonParser.IsArrayHeaderAfterHyphen(first.Content))
        {
            var headerInfo = ToonParser.ParseArrayHeaderLine(first.Content, Constants.DefaultDelimiter, options.Strict);
            if (headerInfo != null)
            {
                cursor.Advance(); // Move past the header line
                return headerInfo.Header.IsKeyedTabular
                    ? DecodeKeyedTabularObject(headerInfo.Header, cursor, 0, options)
                    : DecodeArrayFromHeader(headerInfo.Header, headerInfo.InlineValues, cursor, 0, options);
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

                if (options.Strict)
                {
                    ValidationUtils.AssertNoDuplicateKey(properties.ContainsKey(key), key);
                }

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
        // Check for array header first (before parsing key). Also covers
        // a keyed-tabular field header (spec §9.5), which reuses the same
        // header shape but decodes to an object, not an array.
        var arrayHeader = ToonParser.ParseArrayHeaderLine(content, Constants.DefaultDelimiter, options.Strict);
        if (arrayHeader != null && arrayHeader.Header.Key != null)
        {
            var value = arrayHeader.Header.IsKeyedTabular
                ? DecodeKeyedTabularObject(arrayHeader.Header, cursor, baseDepth, options)
                : DecodeArrayFromHeader(arrayHeader.Header, arrayHeader.InlineValues, cursor, baseDepth, options);
            // After the field, subsequent sibling fields are at baseDepth + 1 (where this field's content is)
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
        int startLine = cursor.Current()?.LineNumber ?? 1;

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
        JsonElement result = header.Fields != null
            ? DecodeTabularArray(header, cursor, baseDepth, options)
            : DecodeListArray(header, cursor, baseDepth, options);

        if (options.Strict)
        {
            // Spec §12: a blank line ANYWHERE inside the array's row/item
            // range is a strict-mode error, not just between the header
            // and the first row — validate over the full span now
            // consumed rather than just that initial gap.
            int endLine = cursor.Current()?.LineNumber ?? int.MaxValue;
            ValidationUtils.ValidateNoBlankLinesInRange(cursor.BlankLines.ToList(), startLine, endLine);
        }

        return result;
    }

    /// <summary>
    /// Decodes a keyed-tabular object (spec §9.5, v4.0.0 RFC #57): a
    /// header using the keyed-seg bracket grammar ("[N:delim?]{fields}:")
    /// whose entry rows each carry their own key, unlike a positional
    /// tabular array (§9.3). Reuses <see cref="BuildTabularRowElement"/>
    /// for the per-entry cell-to-field walk, since an entry row's value
    /// portion decodes exactly like a §9.3 tabular row.
    /// </summary>
    private static JsonElement DecodeKeyedTabularObject(ArrayHeaderInfo header, LineCursor cursor, int baseDepth, DecodeOptions options)
    {
        int startLine = cursor.Current()?.LineNumber ?? 1;

        if (header.Length == 0)
        {
            return JsonDocument.Parse("{}").RootElement;
        }

        var entries = new Dictionary<string, JsonElement>();
        int expectedDepth = baseDepth + 1;
        int leafFieldCount = CountLeafFields(header.Fields!);

        while (cursor.HasMoreAtDepth(expectedDepth))
        {
            var line = cursor.PeekAtDepth(expectedDepth);
            if (line == null)
                break;

            // Spec §9.5 (authoritative line classification at entry
            // depth): every line at entry depth containing an unquoted
            // colon is an entry row. Unlike §9.3 tabular rows, the
            // colon-before-delimiter disambiguation does NOT apply — a
            // keyed scope ends only on depth decrease or end of input.
            if (!ToonParser.HasUnquotedColon(line.Content))
            {
                if (options.Strict)
                {
                    throw new InvalidOperationException($"Line {line.LineNumber}: expected a keyed tabular entry row (\"key: values\")");
                }

                cursor.Advance();
                continue;
            }

            cursor.Advance();

            var keyResult = ToonParser.ParseKeyToken(line.Content, 0);
#if NETSTANDARD2_0
            string cellsContent = line.Content.Substring(keyResult.End).Trim();
#else
            string cellsContent = line.Content[keyResult.End..].Trim();
#endif

            var values = ToonParser.ParseDelimitedValues(cellsContent, header.Delimiter);
            var primitives = ToonParser.MapRowValuesToPrimitives(values);

            // Spec §9.5: strict mode MUST enforce each entry row's cell
            // count equals the leaf-field count.
            if (options.Strict)
            {
                ValidationUtils.AssertExpectedCount(leafFieldCount, primitives.Length, "keyed tabular entry cells");
                ValidationUtils.AssertNoDuplicateKey(entries.ContainsKey(keyResult.Key), keyResult.Key);
            }

            int cellIndex = 0;
            entries[keyResult.Key] = BuildTabularRowElement(header.Fields!, primitives, ref cellIndex);
        }

        if (options.Strict)
        {
            // Spec §9.5: strict mode MUST enforce the entry-row count
            // equals the declared entry count N.
            ValidationUtils.AssertExpectedCount(header.Length, entries.Count, "keyed tabular entries");

            int endLine = cursor.Current()?.LineNumber ?? int.MaxValue;
            ValidationUtils.ValidateNoBlankLinesInRange(cursor.BlankLines.ToList(), startLine, endLine);
        }

        return JsonDocument.Parse(JsonSerializer.Serialize(entries)).RootElement;
    }

    /// <summary>
    /// Decodes a tabular array.
    /// </summary>
    private static JsonElement DecodeTabularArray(ArrayHeaderInfo header, LineCursor cursor, int baseDepth, DecodeOptions options)
    {
        var rows = new List<JsonElement>();
        int expectedDepth = baseDepth + 1;
        int leafFieldCount = CountLeafFields(header.Fields!);

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

            // Spec §9.3: in strict mode, each row's cell count MUST equal
            // the header's leaf-field count (the depth-first, pre-order
            // count across any nested field groups, not just the
            // top-level field count).
            if (options.Strict)
            {
                ValidationUtils.AssertExpectedCount(leafFieldCount, primitives.Length, "tabular row cells");
            }

            int cellIndex = 0;
            rows.Add(BuildTabularRowElement(header.Fields!, primitives, ref cellIndex));
        }

        if (options.Strict)
        {
            ValidationUtils.ValidateNoExtraTabularRows(header.Length, rows.Count);
        }

        return JsonDocument.Parse(JsonSerializer.Serialize(rows)).RootElement;
    }

    /// <summary>
    /// Counts the leaf fields in a tabular field list, walking any nested
    /// field groups (spec §9.3, v4.0.0 RFC #46) depth-first.
    /// </summary>
    private static int CountLeafFields(TabularField[] fields)
    {
        int count = 0;
        foreach (var field in fields)
        {
            count += field.Children == null || field.Children.Length == 0
                ? 1
                : CountLeafFields(field.Children);
        }
        return count;
    }

    /// <summary>
    /// Builds one decoded tabular row by walking the field list
    /// depth-first, pre-order: a leaf field consumes the next cell, and a
    /// nested field group materializes a nested object from its own
    /// subfields, applied recursively (spec §9.3).
    /// </summary>
    private static JsonElement BuildTabularRowElement(TabularField[] fields, JsonElement[] cells, ref int cellIndex)
    {
        var row = new Dictionary<string, JsonElement>();

        foreach (var field in fields)
        {
            if (field.Children == null || field.Children.Length == 0)
            {
                row[field.Name] = cellIndex < cells.Length
                    ? cells[cellIndex]
                    : JsonDocument.Parse("null").RootElement;
                cellIndex++;
            }
            else
            {
                row[field.Name] = BuildTabularRowElement(field.Children, cells, ref cellIndex);
            }
        }

        return JsonDocument.Parse(JsonSerializer.Serialize(row)).RootElement;
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
                    // Spec §6: absence of a delimiter suffix means comma,
                    // never inherited from the parent array's delimiter.
                    var nestedHeader = ToonParser.ParseArrayHeaderLine(itemContent, Constants.DefaultDelimiter, options.Strict);
                    if (nestedHeader != null)
                    {
                        var nestedArray = DecodeArrayFromHeader(nestedHeader.Header, nestedHeader.InlineValues, cursor, expectedDepth, options);
                        items.Add(nestedArray);
                        continue;
                    }
                }
                
                if (ToonParser.IsObjectFirstFieldAfterHyphen(itemContent))
                {
                    // Object starting on this line. A tabular array as the
                    // first field is special-cased per spec §10: its rows
                    // sit at hyphenDepth+2, not hyphenDepth+1, so they don't
                    // collide with this object's remaining fields (read
                    // below at expectedDepth+1).
                    var firstFieldHeader = ToonParser.ParseArrayHeaderLine(itemContent, Constants.DefaultDelimiter, options.Strict);
                    string key;
                    JsonElement value;

                    if (firstFieldHeader != null && firstFieldHeader.Header.Key != null && firstFieldHeader.Header.Fields != null)
                    {
                        key = firstFieldHeader.Header.Key;
                        value = DecodeArrayFromHeader(firstFieldHeader.Header, firstFieldHeader.InlineValues, cursor, expectedDepth + 1, options);
                    }
                    else
                    {
                        var result = DecodeKeyValue(itemContent, cursor, expectedDepth, options);
                        key = result.Key;
                        value = result.Value;
                    }

                    var objDict = new Dictionary<string, JsonElement> { [key] = value };

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