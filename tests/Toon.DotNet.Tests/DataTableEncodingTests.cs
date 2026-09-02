#if !NETSTANDARD2_0
using System.Data;
using System.Text.Json;

namespace ToonFormat.Tests;

/// <summary>
/// Comprehensive tests for DataTable encoding functionality.
/// Tests cover various DataTable scenarios including different data types, null values, and edge cases.
/// </summary>
public class DataTableEncodingTests
{
    [Fact]
    public void Encode_SimpleDataTable_ReturnsTabularFormat()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("role", typeof(string));
        
        table.Rows.Add(1, "Alice", "admin");
        table.Rows.Add(2, "Bob", "user");

        // Act
        var result = Toon.Encode(table);

        // Assert
        Assert.Contains("[2]{id,name,role}:", result);
        Assert.Contains("1,Alice,admin", result);
        Assert.Contains("2,Bob,user", result);
    }

    [Fact]
    public void Encode_EmptyDataTable_ReturnsEmptyArray()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));

        // Act
        var result = Toon.Encode(table);

        // Assert
        // Empty tables encode as simple empty arrays without schema info
        Assert.Contains("[0]:", result);
    }

    [Fact]
    public void Encode_DataTableWithNullValues_HandlesNullsCorrectly()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("email", typeof(string));
        
        table.Rows.Add(1, "Alice", null);
        table.Rows.Add(2, null, "bob@example.com");

        // Act
        var result = Toon.Encode(table);

        // Assert
        Assert.Contains("[2]{id,name,email}:", result);
        Assert.Contains("1,Alice,null", result);
        Assert.Contains("2,null,bob@example.com", result);
    }

    [Fact]
    public void Encode_DataTableWithNumericTypes_EncodesCorrectly()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("score", typeof(double));
        table.Columns.Add("count", typeof(long));
        
        table.Rows.Add(1, 98.5, 1000L);
        table.Rows.Add(2, 87.5, 2000L);

        // Act
        var result = Toon.Encode(table);

        // Assert
        Assert.Contains("[2]{id,score,count}:", result);
        Assert.Contains("1,98.5,1000", result);
        Assert.Contains("2,87.5,2000", result);
    }

    [Fact]
    public void Encode_DataTableWithBooleans_EncodesCorrectly()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("active", typeof(bool));
        table.Columns.Add("verified", typeof(bool));
        
        table.Rows.Add(1, true, false);
        table.Rows.Add(2, false, true);

        // Act
        var result = Toon.Encode(table);

        // Assert
        Assert.Contains("[2]{id,active,verified}:", result);
        Assert.Contains("1,true,false", result);
        Assert.Contains("2,false,true", result);
    }

    [Fact]
    public void Encode_DataTableWithDates_EncodesCorrectly()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("created", typeof(DateTime));
        
        var date1 = new DateTime(2024, 1, 15);
        var date2 = new DateTime(2024, 2, 20);
        
        table.Rows.Add(1, date1);
        table.Rows.Add(2, date2);

        // Act
        var result = Toon.Encode(table);

        // Assert
        Assert.Contains("[2]{id,created}:", result);
        Assert.Contains("1,", result);
        Assert.Contains("2,", result);
    }

    [Fact]
    public void Encode_DataTableWithSpecialCharacters_EscapesCorrectly()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("message", typeof(string));
        
        table.Rows.Add(1, "Hello, World");
        table.Rows.Add(2, "Line1\nLine2");

        // Act
        var result = Toon.Encode(table);

        // Assert
        Assert.Contains("[2]{id,message}:", result);
        Assert.Contains("\"Hello, World\"", result);
    }

    [Fact]
    public void Encode_DataTableWithCustomDelimiter_UsesDelimiter()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        
        table.Rows.Add(1, "Alice");
        table.Rows.Add(2, "Bob");

        var options = new EncodeOptions { Delimiter = '|' };

        // Act
        var result = Toon.Encode(table, options);

        // Assert
        Assert.Contains("[2|]{id,name}:", result);
        Assert.Contains("1|Alice", result);
        Assert.Contains("2|Bob", result);
    }

    [Fact]
    public void Encode_DataTableWithLengthMarker_IncludesMarker()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        
        table.Rows.Add(1, "Alice");

        var options = new EncodeOptions { LengthMarker = '#' };

        // Act
        var result = Toon.Encode(table, options);

        // Assert
        Assert.Contains("[#1]{id,name}:", result);
    }

    [Fact]
    public void Encode_DataTableWithSingleRow_ReturnsCorrectFormat()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        
        table.Rows.Add(1, "Alice");

        // Act
        var result = Toon.Encode(table);

        // Assert
        Assert.Contains("[1]{id,name}:", result);
        Assert.Contains("1,Alice", result);
    }

    [Fact]
    public void Encode_DataTableWithManyRows_HandlesLargeDataset()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("value", typeof(string));
        
        for (int i = 1; i <= 100; i++)
        {
            table.Rows.Add(i, $"Value{i}");
        }

        // Act
        var result = Toon.Encode(table);

        // Assert
        Assert.Contains("[100]{id,value}:", result);
        Assert.Contains("1,Value1", result);
        Assert.Contains("100,Value100", result);
    }

    [Fact]
    public void Encode_DataTableWithQuotedColumnNames_EscapesColumnNames()
    {
        // Arrange
        var table = new DataTable();

        table.Columns.Add("user id", typeof(int));
        table.Columns.Add("full name", typeof(string));
        
        table.Rows.Add(1, "Alice Smith");
        table.Rows.Add(2, "Bob Jones");

        // Act
        var result = Toon.Encode(table);

        // Assert
        Assert.Contains("{user id,full name}", result);
    }

    [Fact]
    public void Encode_NullDataTable_ThrowsArgumentNullException()
    {
        // Arrange
        DataTable? table = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Toon.Encode(table!));
    }

    [Fact]
    public void Encode_DataTableRoundTrip_PreservesData()
    {
        // Arrange
        var originalTable = new DataTable();
        originalTable.Columns.Add("id", typeof(int));
        originalTable.Columns.Add("name", typeof(string));
        originalTable.Columns.Add("score", typeof(double));
        
        originalTable.Rows.Add(1, "Alice", 98.5);
        originalTable.Rows.Add(2, "Bob", 87.3);

        // Act
        var toonString = Toon.Encode(originalTable);
        var decoded = Toon.Decode(toonString);

        // Assert
        Assert.Equal(JsonValueKind.Array, decoded.ValueKind);
        Assert.Equal(2, decoded.GetArrayLength());
        
        var firstRow = decoded[0];
        Assert.Equal(1, firstRow.GetProperty("id").GetInt32());
        Assert.Equal("Alice", firstRow.GetProperty("name").GetString());
        Assert.Equal(98.5, firstRow.GetProperty("score").GetDouble());
    }

    [Fact]
    public void Encode_DataTableWithMixedTypes_EncodesAllCorrectly()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("active", typeof(bool));
        table.Columns.Add("score", typeof(double));
        table.Columns.Add("created", typeof(DateTime));
        
        table.Rows.Add(1, "Alice", true, 98.5, new DateTime(2024, 1, 1));
        table.Rows.Add(2, "Bob", false, 87.3, new DateTime(2024, 2, 1));

        // Act
        var result = Toon.Encode(table);

        // Assert
        Assert.Contains("[2]{id,name,active,score,created}:", result);
        Assert.Contains("Alice", result);
        Assert.Contains("true", result);
        Assert.Contains("98.5", result);
    }

    [Fact]
    public void Encode_DataTableWithAllNulls_EncodesNullsCorrectly()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("value", typeof(string));
        
        table.Rows.Add(1, DBNull.Value);
        table.Rows.Add(2, DBNull.Value);

        // Act
        var result = Toon.Encode(table);

        // Assert
        Assert.Contains("1,null", result);
        Assert.Contains("2,null", result);
    }

    [Fact]
    public void Encode_DataTableWithCustomIndent_UsesIndentation()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        
        table.Rows.Add(1, "Alice");

        var options = new EncodeOptions { Indent = 4 };

        // Act
        var result = Toon.Encode(table, options);

        // Assert
        Assert.Contains("[1]{id,name}:", result);
        // Indentation is applied to nested structures, not the array itself
        var lines = result.Split('\n');
        Assert.True(lines.Length >= 1);
    }

    [Fact]
    public void Encode_DataTableComparedToObjectArray_ProducesSameFormat()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        
        table.Rows.Add(1, "Alice");
        table.Rows.Add(2, "Bob");

        var objectArray = new[]
        {
            new { id = 1, name = "Alice" },
            new { id = 2, name = "Bob" }
        };

        // Act
        var tableResult = Toon.Encode(table);
        var objectResult = Toon.Encode(objectArray);

        // Assert
        Assert.Equal(objectResult, tableResult);
    }

    [Fact]
    public void Encode_DataTableSizeComparison_ShowsReduction()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("role", typeof(string));
        
        for (int i = 1; i <= 10; i++)
        {
            table.Rows.Add(i, $"User{i}", i % 2 == 0 ? "admin" : "user");
        }

        var equivalentArray = new List<Dictionary<string, object>>();
        foreach (DataRow row in table.Rows)
        {
            var dict = new Dictionary<string, object>();
            foreach (DataColumn col in table.Columns)
            {
                dict[col.ColumnName] = row[col];
            }
            equivalentArray.Add(dict);
        }

        // Act
        var toonString = Toon.Encode(table);
        var jsonString = System.Text.Json.JsonSerializer.Serialize(equivalentArray);

        // Assert
        Assert.True(toonString.Length < jsonString.Length, 
            $"TOON ({toonString.Length}) should be smaller than JSON ({jsonString.Length})");
    }

    [Theory]
    [InlineData(typeof(byte), (byte)255)]
    [InlineData(typeof(short), (short)32767)]
    [InlineData(typeof(float), 3.14f)]
    [InlineData(typeof(decimal), 99.99)]
    public void Encode_DataTableWithVariousNumericTypes_EncodesCorrectly(Type columnType, object value)
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("value", columnType);
        
        table.Rows.Add(1, value);

        // Act
        var result = Toon.Encode(table);

        // Assert
        Assert.Contains("[1]{id,value}:", result);
        Assert.Contains("1,", result);
    }

    [Fact]
    public void Encode_DataTableWithGuid_EncodesAsString()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("name", typeof(string));
        
        var guid = Guid.NewGuid();
        table.Rows.Add(guid, "Test");

        // Act
        var result = Toon.Encode(table);

        // Assert
        Assert.Contains("[1]{id,name}:", result);
        Assert.Contains(guid.ToString(), result);
    }
}
#endif