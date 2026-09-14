using System.Text.Json;
using ToonFormat.Shared;

namespace ToonFormat.Encode;

/// <summary>
/// Main encoder class for converting JsonElement values to TOON format.
/// </summary>
internal static class ToonEncoder
{
    /// <summary>
    /// Encodes a JsonElement value to TOON format string.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="options">Encoding options.</param>
    /// <returns>The TOON format string representation.</returns>
    public static string EncodeValue(JsonElement value, EncodeOptions options)
    {
        if (Normalizer.IsJsonPrimitive(value))
        {
            return Primitives.EncodePrimitive(value, options.Delimiter, options.SpecVersion);
        }

        var writer = new LineWriter(options.Indent);

        if (Normalizer.IsJsonArray(value))
        {
            var keys = value.EnumerateArray().ToArray();

            EncodeArray(null, value, writer, 0, options);
        }
        else if (Normalizer.IsJsonObject(value))
        {
            // Spec §9.5 (v4.0.0 RFC #57): encoders MUST use keyed tabular
            // form for a root object of uniform objects. V3 never
            // qualifies — see ExtractKeyedTabularHeader.
            var keyedFields = ExtractKeyedTabularHeader(value, options.SpecVersion, out var rootEntries);
            if (keyedFields != null)
            {
                EncodeObjectAsKeyedTabular(null, rootEntries, keyedFields, writer, 0, options);
            }
            else
            {
                EncodeObject(value, writer, 0, options);
            }
        }

        return writer.ToString();
    }

    /// <summary>
    /// Encodes a JsonElement object.
    /// </summary>
    private static void EncodeObject(JsonElement value, LineWriter writer, int depth, EncodeOptions options)
    {
        foreach (var property in value.EnumerateObject())
        {
            EncodeKeyValuePair(property.Name, property.Value, writer, depth, options);
        }
    }

    /// <summary>
    /// Encodes a key-value pair.
    /// </summary>
    private static void EncodeKeyValuePair(string key, JsonElement value, LineWriter writer, int depth, EncodeOptions options)
    {
        string encodedKey = Primitives.EncodeKey(key);

        if (Normalizer.IsJsonPrimitive(value))
        {
            writer.Push(depth, $"{encodedKey}: {Primitives.EncodePrimitive(value, options.Delimiter, options.SpecVersion)}");
        }
        else if (Normalizer.IsJsonArray(value))
        {
            EncodeArray(key, value, writer, depth, options);
        }
        else if (Normalizer.IsJsonObject(value))
        {
            var properties = value.EnumerateObject().ToArray();
            if (properties.Length == 0)
            {
                // Empty object
                writer.Push(depth, $"{encodedKey}:");
            }
            else
            {
                // Spec §9.5 (v4.0.0 RFC #57): encoders MUST use keyed
                // tabular form for an object-field-position object of
                // uniform objects (an array element never qualifies —
                // §10 — so this detection is intentionally not wired
                // into the list-item encoding paths below). V3 never
                // qualifies — see ExtractKeyedTabularHeader.
                var keyedFields = ExtractKeyedTabularHeader(value, options.SpecVersion, out var entries);
                if (keyedFields != null)
                {
                    EncodeObjectAsKeyedTabular(key, entries, keyedFields, writer, depth, options);
                }
                else
                {
                    writer.Push(depth, $"{encodedKey}:");
                    EncodeObject(value, writer, depth + 1, options);
                }
            }
        }
    }

    /// <summary>
    /// Extracts a keyed-tabular field list (spec §9.5, v4.0.0 RFC #57)
    /// from an object whose values are all uniform non-empty objects,
    /// reusing the same column-uniformity logic as array-of-objects
    /// tabular detection (§9.3). Returns null (and detection doesn't
    /// apply) when <paramref name="specVersion"/> is <see cref="ToonSpecVersion.V3"/>
    /// (keyed tabular form doesn't exist in v3.3.2 at all), or when the
    /// object has fewer than two entries or its values aren't uniform.
    /// </summary>
    private static TabularField[]? ExtractKeyedTabularHeader(JsonElement obj, ToonSpecVersion specVersion, out JsonProperty[] entries)
    {
        entries = obj.EnumerateObject().ToArray();

        if (specVersion == ToonSpecVersion.V3)
            return null;

        // Spec §9.5: detection requires at least two entries.
        if (entries.Length < 2)
            return null;

        var entryValues = entries.Select(e => e.Value).ToArray();
        if (!entryValues.All(v => Normalizer.IsJsonObject(v) && v.EnumerateObject().Any()))
            return null;

        var firstProperties = entryValues[0].EnumerateObject().ToArray();
        if (firstProperties.Length == 0)
            return null;

        var fields = new List<TabularField>();
        foreach (var property in firstProperties)
        {
            var field = BuildTabularFieldOrNull(property.Name, entryValues, specVersion);
            if (field == null)
                return null;
            fields.Add(field);
        }

        string[] fieldNames = fields.Select(f => f.Name).ToArray();
        if (!AllRowsHaveExactKeySet(entryValues, fieldNames))
            return null;

        return fields.ToArray();
    }

    /// <summary>
    /// Writes an object as keyed tabular form: a header declaring the
    /// entry count and field list, followed by one "entrykey: cells" row
    /// per entry in encounter order (spec §9.5).
    /// </summary>
    private static void EncodeObjectAsKeyedTabular(string? key, JsonProperty[] entries, TabularField[] fields, LineWriter writer, int depth, EncodeOptions options)
    {
        string header = Primitives.FormatKeyedHeader(entries.Length, key, options.Delimiter, fields);
        writer.Push(depth, header);

        foreach (var entry in entries)
        {
            var values = new List<JsonElement>();
            CollectTabularRowValues(entry.Value, fields, values);

            string joinedValue = Primitives.EncodeAndJoinPrimitives(values.ToArray(), options.Delimiter, options.SpecVersion);
            string encodedEntryKey = Primitives.EncodeKey(entry.Name);
            writer.Push(depth + 1, $"{encodedEntryKey}: {joinedValue}");
        }
    }

    /// <summary>
    /// Encodes a JsonElement array.
    /// </summary>
    private static void EncodeArray(string? key, JsonElement value, LineWriter writer, int depth, EncodeOptions options)
    {
        var elements = value.EnumerateArray().ToArray();

        if (elements.Length == 0)
        {
            string header = Primitives.FormatHeader(0, key, options.Delimiter, options.LengthMarker);
            writer.Push(depth, header);
            return;
        }

        // Primitive array
        if (Normalizer.IsArrayOfPrimitives(value))
        {
            string formatted = Primitives.FormatInlineArrayLine(elements, options.Delimiter, key, options.LengthMarker, options.SpecVersion);
            writer.Push(depth, formatted);
            return;
        }

        // Array of arrays (all primitives)
        if (Normalizer.IsArrayOfArrays(value))
        {
            bool allPrimitiveArrays = elements.All(Normalizer.IsArrayOfPrimitives);
            if (allPrimitiveArrays)
            {
                EncodeArrayOfArraysAsListItems(key, elements, writer, depth, options);
                return;
            }
        }

        // Array of objects
        if (Normalizer.IsArrayOfObjects(value))
        {
            var header = ExtractTabularHeader(elements, options.SpecVersion);
            if (header != null)
            {
                EncodeArrayOfObjectsAsTabular(key, elements, header, writer, depth, options);
            }
            else
            {
                EncodeMixedArrayAsListItems(key, elements, writer, depth, options);
            }
            return;
        }

        // Mixed array: fallback to expanded format
        EncodeMixedArrayAsListItems(key, elements, writer, depth, options);
    }

    /// <summary>
    /// Encodes an array of arrays as list items.
    /// </summary>
    private static void EncodeArrayOfArraysAsListItems(string? key, JsonElement[] values, LineWriter writer, int depth, EncodeOptions options)
    {
        string header = Primitives.FormatHeader(values.Length, key, options.Delimiter, options.LengthMarker);
        writer.Push(depth, header);

        foreach (var arr in values)
        {
            if (Normalizer.IsArrayOfPrimitives(arr))
            {
                var elements = arr.EnumerateArray().ToArray();
                string inline = Primitives.FormatInlineArrayLine(elements, options.Delimiter, null, options.LengthMarker, options.SpecVersion);
                writer.Push(depth + 1, $"{Constants.ListItemPrefix}{inline}");
            }
        }
    }

    /// <summary>
    /// Encodes an array of objects as a tabular format.
    /// </summary>
    private static void EncodeArrayOfObjectsAsTabular(string? key, JsonElement[] rows, TabularField[] header, LineWriter writer, int depth, EncodeOptions options)
    {
        string formattedHeader = Primitives.FormatHeader(rows.Length, key, options.Delimiter, options.LengthMarker, header);
        writer.Push(depth, formattedHeader);

        WriteTabularRows(rows, header, writer, depth + 1, options);
    }

    /// <summary>
    /// Writes tabular rows for an array of objects, flattening any nested
    /// field groups (spec §9.3, v4.0.0 RFC #46) via a depth-first,
    /// pre-order walk of the field list.
    /// </summary>
    private static void WriteTabularRows(JsonElement[] rows, TabularField[] header, LineWriter writer, int depth, EncodeOptions options)
    {
        foreach (var row in rows)
        {
            var values = new List<JsonElement>();
            CollectTabularRowValues(row, header, values);

            string joinedValue = Primitives.EncodeAndJoinPrimitives(values.ToArray(), options.Delimiter, options.SpecVersion);
            writer.Push(depth, joinedValue);
        }
    }

    /// <summary>
    /// Appends one row's leaf cell values to <paramref name="values"/> in
    /// depth-first, pre-order field-list order: a leaf field contributes
    /// its own value, and a nested field group recurses into its
    /// subfields' values within the row's nested sub-object.
    /// </summary>
    private static void CollectTabularRowValues(JsonElement row, TabularField[] fields, List<JsonElement> values)
    {
        foreach (var field in fields)
        {
            if (field.Children == null || field.Children.Length == 0)
            {
                values.Add(row.TryGetProperty(field.Name, out JsonElement value)
                    ? value
                    : JsonDocument.Parse("null").RootElement);
            }
            else
            {
                JsonElement subRow = row.TryGetProperty(field.Name, out JsonElement subValue)
                    ? subValue
                    : JsonDocument.Parse("{}").RootElement;
                CollectTabularRowValues(subRow, field.Children, values);
            }
        }
    }

    /// <summary>
    /// Extracts a tabular field list (spec §9.3) from an array of objects
    /// if they have uniform structure, including nested-uniform columns
    /// as nested field groups (v4.0.0 RFC #46, only considered when
    /// <paramref name="specVersion"/> is <see cref="ToonSpecVersion.V4"/>
    /// — v3.3.2 has no concept of nested field groups, so under V3 any
    /// non-primitive column disqualifies the whole array from tabular
    /// form, exactly as it did before nested field groups existed).
    /// Returns null if the array doesn't qualify for tabular form at all.
    /// </summary>
    private static TabularField[]? ExtractTabularHeader(JsonElement[] rows, ToonSpecVersion specVersion)
    {
        if (rows.Length == 0)
            return null;

        var firstRow = rows[0];
        if (!Normalizer.IsJsonObject(firstRow))
            return null;

        var properties = firstRow.EnumerateObject().ToArray();
        if (properties.Length == 0)
            return null;

        var fields = new List<TabularField>();
        foreach (var property in properties)
        {
            var field = BuildTabularFieldOrNull(property.Name, rows, specVersion);
            if (field == null)
                return null;
            fields.Add(field);
        }

        string[] fieldNames = fields.Select(f => f.Name).ToArray();
        if (!AllRowsHaveExactKeySet(rows, fieldNames))
            return null;

        return fields.ToArray();
    }

    /// <summary>
    /// Builds the tabular field for one column name, recursively
    /// descending into nested-uniform object columns as nested field
    /// groups when <paramref name="specVersion"/> is <see cref="ToonSpecVersion.V4"/>.
    /// Under <see cref="ToonSpecVersion.V3"/>, a non-primitive column
    /// always disqualifies the whole array from tabular form (spec §9.3
    /// as it exists at v3.3.2, before nested-uniform columns/RFC #46).
    /// Returns null when the column disqualifies the array from tabular
    /// form.
    /// </summary>
    private static TabularField? BuildTabularFieldOrNull(string name, JsonElement[] rows, ToonSpecVersion specVersion)
    {
        var columnValues = new JsonElement[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            if (!rows[i].TryGetProperty(name, out JsonElement value))
                return null;
            columnValues[i] = value;
        }

        if (columnValues.All(Normalizer.IsJsonPrimitive))
        {
            return new TabularField { Name = name, Children = null };
        }

        if (specVersion == ToonSpecVersion.V3)
            return null;

        // Nested-uniform (v4.0.0 RFC #46): every value is a non-empty
        // object, all with the same key set, and every sub-column is
        // itself uniform-primitive or nested-uniform (recursively,
        // unbounded depth).
        bool allNonEmptyObjects = columnValues.All(v =>
            Normalizer.IsJsonObject(v) && v.EnumerateObject().Any());
        if (!allNonEmptyObjects)
            return null;

        var firstSubProperties = columnValues[0].EnumerateObject().ToArray();

        var subFields = new List<TabularField>();
        foreach (var subProperty in firstSubProperties)
        {
            var subField = BuildTabularFieldOrNull(subProperty.Name, columnValues, specVersion);
            if (subField == null)
                return null;
            subFields.Add(subField);
        }

        string[] subFieldNames = subFields.Select(f => f.Name).ToArray();
        if (!AllRowsHaveExactKeySet(columnValues, subFieldNames))
            return null;

        return new TabularField { Name = name, Children = subFields.ToArray() };
    }

    /// <summary>
    /// Checks that every row is an object with exactly the given set of
    /// keys (order may vary per row, per spec §9.3).
    /// </summary>
    private static bool AllRowsHaveExactKeySet(JsonElement[] rows, string[] keys)
    {
        foreach (var row in rows)
        {
            if (!Normalizer.IsJsonObject(row))
                return false;

            var properties = row.EnumerateObject().ToArray();
            if (properties.Length != keys.Length)
                return false;

            foreach (string key in keys)
            {
                if (!row.TryGetProperty(key, out _))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Encodes mixed arrays as list items.
    /// </summary>
    private static void EncodeMixedArrayAsListItems(string? key, JsonElement[] items, LineWriter writer, int depth, EncodeOptions options)
    {
        string header = Primitives.FormatHeader(items.Length, key, options.Delimiter, options.LengthMarker);
        writer.Push(depth, header);

        foreach (var item in items)
        {
            EncodeListItemValue(item, writer, depth + 1, options);
        }
    }

    /// <summary>
    /// Encodes a value as a list item.
    /// </summary>
    private static void EncodeListItemValue(JsonElement value, LineWriter writer, int depth, EncodeOptions options)
    {
        if (Normalizer.IsJsonPrimitive(value))
        {
            writer.Push(depth, $"{Constants.ListItemPrefix}{Primitives.EncodePrimitive(value, options.Delimiter, options.SpecVersion)}");
        }
        else if (Normalizer.IsJsonArray(value) && Normalizer.IsArrayOfPrimitives(value))
        {
            var elements = value.EnumerateArray().ToArray();
            string inline = Primitives.FormatInlineArrayLine(elements, options.Delimiter, null, options.LengthMarker, options.SpecVersion);
            writer.Push(depth, $"{Constants.ListItemPrefix}{inline}");
        }
        else if (Normalizer.IsJsonArray(value))
        {
            // A non-primitive array as a list item (e.g. an array-of-arrays,
            // or an array of objects that isn't tabular-eligible): spec
            // §9.2/§9.4 requires the expanded "- [M<delim?>]:" header form
            // with items at depth+1, not the inline shorthand above.
            var elements = value.EnumerateArray().ToArray();
            var tabularHeader = Normalizer.IsArrayOfObjects(value) ? ExtractTabularHeader(elements, options.SpecVersion) : null;

            if (tabularHeader != null)
            {
                string formattedHeader = Primitives.FormatHeader(elements.Length, null, options.Delimiter, options.LengthMarker, tabularHeader);
                writer.Push(depth, $"{Constants.ListItemPrefix}{formattedHeader}");
                WriteTabularRows(elements, tabularHeader, writer, depth + 1, options);
            }
            else
            {
                string header = Primitives.FormatHeader(elements.Length, null, options.Delimiter, options.LengthMarker);
                writer.Push(depth, $"{Constants.ListItemPrefix}{header}");

                foreach (var item in elements)
                {
                    EncodeListItemValue(item, writer, depth + 1, options);
                }
            }
        }
        else if (Normalizer.IsJsonObject(value))
        {
            EncodeObjectAsListItem(value, writer, depth, options);
        }
    }

    /// <summary>
    /// Encodes an object as a list item.
    /// </summary>
    private static void EncodeObjectAsListItem(JsonElement obj, LineWriter writer, int depth, EncodeOptions options)
    {
        var properties = obj.EnumerateObject().ToArray();
        if (properties.Length == 0)
        {
            writer.Push(depth, Constants.ListItemPrefix.TrimEnd());
            return;
        }

        // First key-value on the same line as "- "
        var firstProperty = properties[0];
        string encodedKey = Primitives.EncodeKey(firstProperty.Name);
        JsonElement firstValue = firstProperty.Value;

        if (Normalizer.IsJsonPrimitive(firstValue))
        {
            writer.Push(depth, $"{Constants.ListItemPrefix}{encodedKey}: {Primitives.EncodePrimitive(firstValue, options.Delimiter, options.SpecVersion)}");
        }
        else if (Normalizer.IsJsonArray(firstValue))
        {
            if (Normalizer.IsArrayOfPrimitives(firstValue))
            {
                // Inline format for primitive arrays
                var elements = firstValue.EnumerateArray().ToArray();
                string formatted = Primitives.FormatInlineArrayLine(elements, options.Delimiter, firstProperty.Name, options.LengthMarker, options.SpecVersion);
                writer.Push(depth, $"{Constants.ListItemPrefix}{formatted}");
            }
            else if (Normalizer.IsArrayOfObjects(firstValue))
            {
                // Check if array of objects can use tabular format
                var arrayElements = firstValue.EnumerateArray().ToArray();
                var header = ExtractTabularHeader(arrayElements, options.SpecVersion);
                if (header != null)
                {
                    // Tabular format for uniform arrays of objects. Spec
                    // §10: rows sit at depth+2 so they don't collide with
                    // this object's remaining fields, written at depth+1
                    // below.
                    string formattedHeader = Primitives.FormatHeader(arrayElements.Length, firstProperty.Name, options.Delimiter, options.LengthMarker, header);
                    writer.Push(depth, $"{Constants.ListItemPrefix}{formattedHeader}");
                    WriteTabularRows(arrayElements, header, writer, depth + 2, options);
                }
                else
                {
                    // Fall back to list format for non-uniform arrays of objects
                    writer.Push(depth, $"{Constants.ListItemPrefix}{encodedKey}[{arrayElements.Length}]:");
                    foreach (var item in arrayElements)
                    {
                        EncodeObjectAsListItem(item, writer, depth + 1, options);
                    }
                }
            }
            else
            {
                // Complex arrays on separate lines
                var elements = firstValue.EnumerateArray().ToArray();
                writer.Push(depth, $"{Constants.ListItemPrefix}{encodedKey}[{elements.Length}]:");

                // Encode array contents at depth + 1
                foreach (var item in elements)
                {
                    EncodeListItemValue(item, writer, depth + 1, options);
                }
            }
        }
        else if (Normalizer.IsJsonObject(firstValue))
        {
            var nestedProperties = firstValue.EnumerateObject().ToArray();
            if (nestedProperties.Length == 0)
            {
                writer.Push(depth, $"{Constants.ListItemPrefix}{encodedKey}:");
            }
            else
            {
                writer.Push(depth, $"{Constants.ListItemPrefix}{encodedKey}:");
                EncodeObject(firstValue, writer, depth + 2, options);
            }
        }

        // Remaining keys on indented lines
        for (int i = 1; i < properties.Length; i++)
        {
            var property = properties[i];
            EncodeKeyValuePair(property.Name, property.Value, writer, depth + 1, options);
        }
    }
}