using System.Data;
using System.Dynamic;
using System.Text.Json;

namespace ToonFormat.Shared;

/// <summary>
/// Utility methods for working with DataTable objects.
/// </summary>
internal static class TableUtils
{
    /// <summary>
    /// Converts a DataTable to a list of dynamic objects using ExpandoObject.
    /// This is ideal when column names and types are only known at runtime.
    /// The result can be efficiently encoded to TOON tabular format.
    /// </summary>
    /// <param name="table">The DataTable to convert.</param>
    /// <returns>A list of dynamic objects that encode efficiently to TOON.</returns>
    /// <remarks>
    /// This method is preferred over Dictionary when you want the TOON encoder
    /// to produce the most space-efficient tabular format.
    /// </remarks>
    /// <example>
    /// <code>
    /// DataTable table = GetDataFromDatabase(); // Unknown schema
    /// var rows = table.ToDynamicList();
    /// string toon = Toon.Encode(rows);
    /// // Result: [100]{id,name,email}:
    /// //   1,Alice,alice@example.com
    /// //   2,Bob,bob@example.com
    /// //   ...
    /// </code>
    /// </example>
    public static List<dynamic> ToDynamicList(this DataTable table)
    {
        if (table == null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        var list = new List<dynamic>();

        if (table.Rows.Count == 0)
        {
            var columnNames = new List<string>();
            foreach (DataColumn column in table.Columns)
            {
                columnNames.Add(column.ColumnName);
            }
            list.Add(columnNames);
        }
        else
        {
            foreach (DataRow row in table.Rows)
            {
                var expando = new ExpandoObject();
                var dictionary = (IDictionary<string, object?>)expando;

                foreach (DataColumn column in table.Columns)
                {
                    object? value = row[column];
                    dictionary[column.ColumnName] = value is DBNull ? null : value;
                }

                list.Add(expando);
            }
        }

        return list;
    }


    /// <summary>
    /// Converts a DataTable to a list of dictionaries suitable for TOON encoding.
    /// Each row becomes a dictionary with column names as keys.
    /// DBNull values are converted to null.
    /// </summary>
    /// <param name="table">The DataTable to convert.</param>
    /// <returns>A list of dictionaries representing the table rows.</returns>
    public static List<Dictionary<string, object?>> ToList(this DataTable table)
    {
        if (table == null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        var list = new List<Dictionary<string, object?>>();

        if (table.Rows.Count == 0)
        {
            foreach (DataColumn column in table.Columns)
            {
                list.Add(new Dictionary<string, object?>() { { column.ColumnName, null } });
            }
        }
        else
        {
            foreach (DataRow row in table.Rows)
            {
                var rowDict = new Dictionary<string, object?>();
                foreach (DataColumn col in table.Columns)
                {
                    var colValue = row.IsNull(col.ColumnName) ? DBNull.Value : row[col.ColumnName];
                    rowDict.Add(col.ColumnName, colValue);
                }
                list.Add(rowDict);
            }
        }
        
        return list;
    }

    /// <summary>
    /// Converts a DataTable to a strongly-typed list of objects.
    /// This method uses reflection to map column names to property names.
    /// </summary>
    /// <typeparam name="T">The type to convert each row to. Must have a parameterless constructor.</typeparam>
    /// <param name="table">The DataTable to convert.</param>
    /// <returns>A list of objects of type T.</returns>
    public static List<T> ToList<T>(this DataTable table) where T : new()
    {
        if (table == null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        var list = new List<T>();
        var properties = typeof(T).GetProperties();

        foreach (DataRow row in table.Rows)
        {
            var item = new T();
            
            foreach (DataColumn column in table.Columns)
            {
                // Find matching property (case-insensitive)
                var property = properties.FirstOrDefault(p => 
                    string.Equals(p.Name, column.ColumnName, StringComparison.OrdinalIgnoreCase));
                
                if (property != null && property.CanWrite)
                {
                    object? value = row[column];
                    
                    // Handle DBNull
                    if (value is DBNull)
                    {
                        value = null;
                    }
                    
                    // Only set if value is assignable to property type
                    if (value == null || property.PropertyType.IsAssignableFrom(value.GetType()))
                    {
                        property.SetValue(item, value);
                    }
                    else
                    {
                        // Try to convert value to property type
                        try
                        {
                            var convertedValue = Convert.ChangeType(value, property.PropertyType);
                            property.SetValue(item, convertedValue);
                        }
                        catch
                        {
                            // Ignore conversion errors and leave property at default value
                        }
                    }
                }
            }
            
            list.Add(item);
        }
        
        return list;
    }

    /// <summary>
    /// Gets the column names from a DataTable.
    /// </summary>
    /// <param name="table">The DataTable to get column names from.</param>
    /// <returns>An array of column names.</returns>
    public static string[] GetColumnNames(this DataTable table)
    {
        if (table == null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        return table.Columns.Cast<DataColumn>()
            .Select(c => c.ColumnName)
            .ToArray();
    }

    /// <summary>
    /// Checks if a DataTable is empty (has no rows).
    /// </summary>
    /// <param name="table">The DataTable to check.</param>
    /// <returns>True if the table has no rows, false otherwise.</returns>
    public static bool IsEmpty(this DataTable table)
    {
        if (table == null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        return table.Rows.Count == 0;
    }

    /// <summary>
    /// Checks if all values in a DataTable are primitive types suitable for tabular TOON format.
    /// </summary>
    /// <param name="table">The DataTable to check.</param>
    /// <returns>True if all column types are primitive, false otherwise.</returns>
    public static bool HasOnlyPrimitiveTypes(this DataTable table)
    {
        if (table == null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        foreach (DataColumn column in table.Columns)
        {
            var type = column.DataType;
            
            // Check if type is a primitive or common value type
            if (!IsPrimitiveType(type))
            {
                return false;
            }
        }
        
        return true;
    }

    /// <summary>
    /// Determines if a type is considered primitive for TOON encoding.
    /// </summary>
    private static bool IsPrimitiveType(Type type)
    {
        return type.IsPrimitive
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || type.IsEnum;
    }

    /// <summary>
    /// Converts a DataTable to a JsonElement object.
    /// This method builds the JsonElement directly without using the Normalizer.
    /// </summary>
    /// <param name="table">The DataTable to convert.</param>
    /// <returns>A JsonElement representing the table as an array of objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown when table is null.</exception>
    public static JsonElement ToJsonElement(this DataTable table)
    {
        if (table == null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartArray();

            if (table.Rows.Count == 0)
            {
                writer.WriteStartObject();
                foreach (DataColumn column in table.Columns)
                {                    
                    writer.WritePropertyName(column.ColumnName);
                    //writer.WriteCommentValue("SchemaOnly");
                    writer.WriteNullValue();
                }
                writer.WriteEndObject();
            }
            else
            {
                foreach (DataRow row in table.Rows)
                {
                    writer.WriteStartObject();

                    foreach (DataColumn column in table.Columns)
                    {
                        object? value = row[column];

                        writer.WritePropertyName(column.ColumnName);

                        if (value == null || value is DBNull)
                        {
                            writer.WriteNullValue();
                        }
                        else
                        {
                            WriteJsonValue(writer, value);
                        }
                    }

                    writer.WriteEndObject();
                }
            }

            writer.WriteEndArray();
        }

        stream.Position = 0;
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Writes a value to a Utf8JsonWriter based on its type.
    /// </summary>
    private static void WriteJsonValue(Utf8JsonWriter writer, object value)
    {
        switch (value)
        {
            case string s:
                writer.WriteStringValue(s);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case short sh:
                writer.WriteNumberValue(sh);
                break;
            case byte b:
                writer.WriteNumberValue(b);
                break;
            case sbyte sb:
                writer.WriteNumberValue(sb);
                break;
            case uint ui:
                writer.WriteNumberValue(ui);
                break;
            case ulong ul:
                writer.WriteNumberValue(ul);
                break;
            case ushort us:
                writer.WriteNumberValue(us);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case float f:
                writer.WriteNumberValue(f);
                break;
            case decimal dec:
                writer.WriteNumberValue(dec);
                break;
            case bool bo:
                writer.WriteBooleanValue(bo);
                break;
            case DateTime dt:
                writer.WriteStringValue(dt);
                break;
            case DateTimeOffset dto:
                writer.WriteStringValue(dto);
                break;
            case Guid g:
                writer.WriteStringValue(g);
                break;
            case TimeSpan ts:
                writer.WriteStringValue(ts.ToString());
                break;
            default:
                // For other types, serialize as string
                writer.WriteStringValue(value.ToString());
                break;
        }
    }
}
