using System.Data;
using ToonFormat.Shared;

namespace ToonFormat.Tests;

/// <summary>
/// Comprehensive tests for TableUtils extension methods.
/// Tests cover DataTable conversion, type mapping, and edge cases.
/// </summary>
public class TableUtilsTests
{
    #region ToList<T> (Typed) Tests

    [Fact]
    public void ToListTyped_SimpleDataTable_ReturnsTypedList()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Role", typeof(string));
        table.Rows.Add(1, "Alice", "admin");
        table.Rows.Add(2, "Bob", "user");

        // Act
        var result = table.ToList<TestUser>();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal("admin", result[0].Role);
        Assert.Equal(2, result[1].Id);
        Assert.Equal("Bob", result[1].Name);
    }

    [Fact]
    public void ToListTyped_CaseInsensitiveMapping_MapsCorrectly()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int)); // lowercase
        table.Columns.Add("NAME", typeof(string)); // uppercase
        table.Columns.Add("Role", typeof(string)); // mixed
        table.Rows.Add(1, "Alice", "admin");

        // Act
        var result = table.ToList<TestUser>();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal("admin", result[0].Role);
    }

    [Fact]
    public void ToListTyped_DataTableWithDBNull_SetsPropertyToNull()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add(1, DBNull.Value);

        // Act
        var result = table.ToList<TestUser>();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Null(result[0].Name);
    }

    [Fact]
    public void ToListTyped_EmptyDataTable_ReturnsEmptyList()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));

        // Act
        var result = table.ToList<TestUser>();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ToListTyped_MissingColumns_LeavesPropertiesAtDefault()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        // Missing Name and Role columns
        table.Rows.Add(1);

        // Act
        var result = table.ToList<TestUser>();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Null(result[0].Name); // Default for string
        Assert.Null(result[0].Role);
    }

    [Fact]
    public void ToListTyped_ExtraColumns_IgnoresExtraColumns()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("ExtraColumn", typeof(string)); // Not in TestUser
        table.Rows.Add(1, "Alice", "Extra");

        // Act
        var result = table.ToList<TestUser>();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Alice", result[0].Name);
    }

    [Fact]
    public void ToListTyped_TypeConversion_ConvertsCompatibleTypes()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("Score", typeof(int)); // int in table
        table.Rows.Add(98);

        // Act
        var result = table.ToList<TestScore>();

        // Assert
        Assert.Single(result);
        Assert.Equal(98.0, result[0].Score); // Converted to double
    }

    [Fact]
    public void ToListTyped_IncompatibleType_IgnoresProperty()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("Id", typeof(string)); // String instead of int
        table.Rows.Add("NotANumber");

        // Act
        var result = table.ToList<TestUser>();

        // Assert
        Assert.Single(result);
        Assert.Equal(0, result[0].Id); // Default int value
    }

    [Fact]
    public void ToListTyped_NullDataTable_ThrowsArgumentNullException()
    {
        // Arrange
        DataTable? table = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => table!.ToList<TestUser>());
    }

    [Fact]
    public void ToListTyped_ReadOnlyProperty_SkipsProperty()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("ReadOnlyProperty", typeof(string));
        table.Rows.Add(1, "Test");

        // Act
        var result = table.ToList<TestUserWithReadOnly>();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        // ReadOnlyProperty should remain at default
    }

    #endregion

    #region GetColumnNames Tests

    [Fact]
    public void GetColumnNames_SimpleDataTable_ReturnsColumnNames()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id");
        table.Columns.Add("name");
        table.Columns.Add("role");

        // Act
        var result = table.GetColumnNames();

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("id", result[0]);
        Assert.Equal("name", result[1]);
        Assert.Equal("role", result[2]);
    }

    [Fact]
    public void GetColumnNames_EmptyDataTable_ReturnsEmptyArray()
    {
        // Arrange
        var table = new DataTable();

        // Act
        var result = table.GetColumnNames();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetColumnNames_NullDataTable_ThrowsArgumentNullException()
    {
        // Arrange
        DataTable? table = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => table!.GetColumnNames());
    }

    [Fact]
    public void GetColumnNames_PreservesOrder()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("z");
        table.Columns.Add("a");
        table.Columns.Add("m");

        // Act
        var result = table.GetColumnNames();

        // Assert
        Assert.Equal(new[] { "z", "a", "m" }, result);
    }

    #endregion

    #region IsEmpty Tests

    [Fact]
    public void IsEmpty_EmptyDataTable_ReturnsTrue()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));

        // Act
        var result = table.IsEmpty();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsEmpty_DataTableWithRows_ReturnsFalse()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Rows.Add(1);

        // Act
        var result = table.IsEmpty();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsEmpty_NullDataTable_ThrowsArgumentNullException()
    {
        // Arrange
        DataTable? table = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => table!.IsEmpty());
    }

    #endregion

    #region HasOnlyPrimitiveTypes Tests

    [Fact]
    public void HasOnlyPrimitiveTypes_AllPrimitiveTypes_ReturnsTrue()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("score", typeof(double));
        table.Columns.Add("active", typeof(bool));

        // Act
        var result = table.HasOnlyPrimitiveTypes();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasOnlyPrimitiveTypes_WithDateTime_ReturnsTrue()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("created", typeof(DateTime));

        // Act
        var result = table.HasOnlyPrimitiveTypes();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasOnlyPrimitiveTypes_WithGuid_ReturnsTrue()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("name", typeof(string));

        // Act
        var result = table.HasOnlyPrimitiveTypes();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasOnlyPrimitiveTypes_WithDecimal_ReturnsTrue()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("amount", typeof(decimal));

        // Act
        var result = table.HasOnlyPrimitiveTypes();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasOnlyPrimitiveTypes_WithComplexType_ReturnsFalse()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("data", typeof(object)); // Complex type

        // Act
        var result = table.HasOnlyPrimitiveTypes();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasOnlyPrimitiveTypes_EmptyDataTable_ReturnsTrue()
    {
        // Arrange
        var table = new DataTable();

        // Act
        var result = table.HasOnlyPrimitiveTypes();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasOnlyPrimitiveTypes_NullDataTable_ThrowsArgumentNullException()
    {
        // Arrange
        DataTable? table = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => table!.HasOnlyPrimitiveTypes());
    }

    #endregion
        
    #region Helper Classes

    private class TestUser
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Role { get; set; }
    }

    private class TestScore
    {
        public double Score { get; set; }
    }

    private class TestUserWithReadOnly
    {
        public int Id { get; set; }
        public string ReadOnlyProperty { get; } = "ReadOnly";
    }

    #endregion
}