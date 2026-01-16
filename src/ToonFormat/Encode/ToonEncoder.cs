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
            return Primitives.EncodePrimitive(value, options.Delimiter);
        }

        var writer = new LineWriter(options.Indent);

        if (Normalizer.IsJsonArray(value))
        {
            var keys = value.EnumerateArray().ToArray();
               
            EncodeArray(null, value, writer, 0, options);
        }
        else if (Normalizer.IsJsonObject(value))
        {
            EncodeObject(value, writer, 0, options);
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
            writer.Push(depth, $"{encodedKey}: {Primitives.EncodePrimitive(value, options.Delimiter)}");
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
                writer.Push(depth, $"{encodedKey}:");
                EncodeObject(value, writer, depth + 1, options);
            }
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
            string formatted = Primitives.FormatInlineArrayLine(elements, options.Delimiter, key, options.LengthMarker);
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
            var header = ExtractTabularHeader(elements);
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
                string inline = Primitives.FormatInlineArrayLine(elements, options.Delimiter, null, options.LengthMarker);
                writer.Push(depth + 1, $"{Constants.ListItemPrefix}{inline}");
            }
        }
    }

    /// <summary>
    /// Encodes an array of objects as a tabular format.
    /// </summary>
    private static void EncodeArrayOfObjectsAsTabular(string? key, JsonElement[] rows, string[] header, LineWriter writer, int depth, EncodeOptions options)
    {
        string formattedHeader = Primitives.FormatHeader(rows.Length, key, options.Delimiter, options.LengthMarker, header);
        writer.Push(depth, formattedHeader);

        WriteTabularRows(rows, header, writer, depth + 1, options);
    }

    /// <summary>
    /// Writes tabular rows for an array of objects.
    /// </summary>
    private static void WriteTabularRows(JsonElement[] rows, string[] header, LineWriter writer, int depth, EncodeOptions options)
    {
        foreach (var row in rows)
        {
            var values = new List<JsonElement>();
            foreach (string key in header)
            {
                if (row.TryGetProperty(key, out JsonElement value))
                {
                    values.Add(value);
                }
                else
                {
                    values.Add(JsonDocument.Parse("null").RootElement);
                }
            }
            
            string joinedValue = Primitives.EncodeAndJoinPrimitives(values.ToArray(), options.Delimiter);
            writer.Push(depth, joinedValue);
        }
    }

    /// <summary>
    /// Extracts tabular header from array of objects if they have uniform structure.
    /// </summary>
    private static string[]? ExtractTabularHeader(JsonElement[] rows)
    {
        if (rows.Length == 0)
            return null;

        var firstRow = rows[0];
        if (!Normalizer.IsJsonObject(firstRow))
            return null;

        var properties = firstRow.EnumerateObject().ToArray();
        if (properties.Length == 0)
            return null;

        string[] firstKeys = properties.Select(p => p.Name).ToArray();

        if (IsTabularArray(rows, firstKeys))
        {
            return firstKeys;
        }

        return null;
    }

    /// <summary>
    /// Checks if an array of objects can be represented in tabular format.
    /// </summary>
    private static bool IsTabularArray(JsonElement[] rows, string[] header)
    {
        foreach (var row in rows)
        {
            if (!Normalizer.IsJsonObject(row))
                return false;

            var properties = row.EnumerateObject().ToArray();
            
            // All objects must have the same number of keys
            if (properties.Length != header.Length)
                return false;

            // Check that all header keys exist in the row and all values are primitives
            foreach (string key in header)
            {
                if (!row.TryGetProperty(key, out JsonElement value))
                    return false;
                
                if (!Normalizer.IsJsonPrimitive(value))
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
            writer.Push(depth, $"{Constants.ListItemPrefix}{Primitives.EncodePrimitive(value, options.Delimiter)}");
        }
        else if (Normalizer.IsJsonArray(value) && Normalizer.IsArrayOfPrimitives(value))
        {
            var elements = value.EnumerateArray().ToArray();
            string inline = Primitives.FormatInlineArrayLine(elements, options.Delimiter, null, options.LengthMarker);
            writer.Push(depth, $"{Constants.ListItemPrefix}{inline}");
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
            writer.Push(depth, $"{Constants.ListItemPrefix}{encodedKey}: {Primitives.EncodePrimitive(firstValue, options.Delimiter)}");
        }
        else if (Normalizer.IsJsonArray(firstValue))
        {
            if (Normalizer.IsArrayOfPrimitives(firstValue))
            {
                // Inline format for primitive arrays
                var elements = firstValue.EnumerateArray().ToArray();
                string formatted = Primitives.FormatInlineArrayLine(elements, options.Delimiter, firstProperty.Name, options.LengthMarker);
                writer.Push(depth, $"{Constants.ListItemPrefix}{formatted}");
            }
            else if (Normalizer.IsArrayOfObjects(firstValue))
            {
                // Check if array of objects can use tabular format
                var arrayElements = firstValue.EnumerateArray().ToArray();
                var header = ExtractTabularHeader(arrayElements);
                if (header != null)
                {
                    // Tabular format for uniform arrays of objects
                    string formattedHeader = Primitives.FormatHeader(arrayElements.Length, firstProperty.Name, options.Delimiter, options.LengthMarker, header);
                    writer.Push(depth, $"{Constants.ListItemPrefix}{formattedHeader}");
                    WriteTabularRows(arrayElements, header, writer, depth + 1, options);
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