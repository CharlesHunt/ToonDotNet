#if !NETSTANDARD2_0
using System.Data;

namespace ToonFormat.Tests;

/// <summary>
/// Comprehensive tests for Toon.SaveAsync(DataTable, string, EncodeOptions?, CancellationToken).
/// </summary>
public class DataTableSaveAsyncTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    private string GetTempFilePath(string extension = ".toon")
    {
        var path = Path.Combine(Path.GetTempPath(), $"toon_dt_async_{Guid.NewGuid()}{extension}");
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
                try { File.Delete(file); } catch { }
        }
    }

    // =========================================================================
    // Happy-path — file creation and content
    // =========================================================================

    [Fact]
    public async Task SaveAsync_SimpleDataTable_CreatesFile()
    {
        // Arrange
        var table = BuildSimpleTable();
        var filePath = GetTempFilePath();

        // Act
        await Toon.SaveAsync(table, filePath);

        // Assert
        Assert.True(File.Exists(filePath));
        Assert.NotEmpty(File.ReadAllText(filePath));
    }

    [Fact]
    public async Task SaveAsync_SimpleDataTable_FileContainsValidToon()
    {
        // Arrange
        var table = BuildSimpleTable();
        var filePath = GetTempFilePath();

        // Act
        await Toon.SaveAsync(table, filePath);

        // Assert
        var content = File.ReadAllText(filePath);
        Assert.True(Toon.IsValid(content));
    }

    [Fact]
    public async Task SaveAsync_SimpleDataTable_ContentMatchesSyncEncode()
    {
        // Arrange
        var table = BuildSimpleTable();
        var filePath = GetTempFilePath();
        var expected = Toon.Encode(table);

        // Act
        await Toon.SaveAsync(table, filePath);

        // Assert
        Assert.Equal(expected, File.ReadAllText(filePath));
    }

    [Fact]
    public async Task SaveAsync_SimpleDataTable_ContainsCorrectHeaders()
    {
        // Arrange
        var table = BuildSimpleTable();
        var filePath = GetTempFilePath();

        // Act
        await Toon.SaveAsync(table, filePath);

        // Assert
        var content = File.ReadAllText(filePath);
        Assert.Contains("[2]{id,name,role}:", content);
    }

    [Fact]
    public async Task SaveAsync_SimpleDataTable_ContainsCorrectRows()
    {
        // Arrange
        var table = BuildSimpleTable();
        var filePath = GetTempFilePath();

        // Act
        await Toon.SaveAsync(table, filePath);

        // Assert
        var content = File.ReadAllText(filePath);
        Assert.Contains("1,Alice,admin", content);
        Assert.Contains("2,Bob,user", content);
    }

    // =========================================================================
    // Edge cases — empty table
    // =========================================================================

    [Fact]
    public async Task SaveAsync_EmptyDataTable_CreatesFile()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        var filePath = GetTempFilePath();

        // Act
        await Toon.SaveAsync(table, filePath);

        // Assert
        Assert.True(File.Exists(filePath));
        Assert.NotEmpty(File.ReadAllText(filePath));
    }

    [Fact]
    public async Task SaveAsync_EmptyDataTable_ContentMatchesSyncEncode()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        var filePath = GetTempFilePath();
        var expected = Toon.Encode(table);

        // Act
        await Toon.SaveAsync(table, filePath);

        // Assert
        Assert.Equal(expected, File.ReadAllText(filePath));
    }

    // =========================================================================
    // Null / invalid argument guards
    // =========================================================================

    [Fact]
    public async Task SaveAsync_NullTable_ThrowsArgumentNullException()
    {
        // Arrange
        var filePath = GetTempFilePath();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Toon.SaveAsync((DataTable)null!, filePath));
    }

    [Fact]
    public async Task SaveAsync_NullFilePath_ThrowsArgumentException()
    {
        // Arrange
        var table = BuildSimpleTable();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Toon.SaveAsync(table, null!));
    }

    [Fact]
    public async Task SaveAsync_EmptyFilePath_ThrowsArgumentException()
    {
        // Arrange
        var table = BuildSimpleTable();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Toon.SaveAsync(table, string.Empty));
    }

    // =========================================================================
    // EncodeOptions forwarded correctly
    // =========================================================================

    [Fact]
    public async Task SaveAsync_WithCustomDelimiter_UsesDelimiter()
    {
        // Arrange
        var table = BuildSimpleTable();
        var filePath = GetTempFilePath();
        var options = new EncodeOptions { Delimiter = '|' };

        // Act
        await Toon.SaveAsync(table, filePath, options);

        // Assert
        var content = File.ReadAllText(filePath);
        Assert.Contains("|", content);
        Assert.Equal(Toon.Encode(table, options), content);
    }

    [Fact]
    public async Task SaveAsync_WithCustomIndent_ContentMatchesSyncEncode()
    {
        // Arrange
        var table = BuildSimpleTable();
        var filePath = GetTempFilePath();
        var options = new EncodeOptions { Indent = 4 };

        // Act
        await Toon.SaveAsync(table, filePath, options);

        // Assert
        Assert.Equal(Toon.Encode(table, options), File.ReadAllText(filePath));
    }

    // =========================================================================
    // CancellationToken
    // =========================================================================

    [Fact]
    public async Task SaveAsync_WithCancellationToken_Completes()
    {
        // Arrange
        var table = BuildSimpleTable();
        var filePath = GetTempFilePath();
        using var cts = new CancellationTokenSource();

        // Act
        await Toon.SaveAsync(table, filePath, cancellationToken: cts.Token);

        // Assert
        Assert.True(File.Exists(filePath));
    }

    // =========================================================================
    // Data-type coverage
    // =========================================================================

    [Fact]
    public async Task SaveAsync_DataTableWithNullValues_HandlesNulls()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("email", typeof(string));
        table.Rows.Add(1, "Alice", null);
        table.Rows.Add(2, null, "bob@example.com");
        var filePath = GetTempFilePath();

        // Act
        await Toon.SaveAsync(table, filePath);

        // Assert
        var content = File.ReadAllText(filePath);
        Assert.Contains("[2]{id,name,email}:", content);
        Assert.Equal(Toon.Encode(table), content);
    }

    [Fact]
    public async Task SaveAsync_DataTableWithNumericTypes_EncodesCorrectly()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("score", typeof(double));
        table.Columns.Add("count", typeof(long));
        table.Rows.Add(1, 98.5, 1000L);
        table.Rows.Add(2, 87.5, 2000L);
        var filePath = GetTempFilePath();

        // Act
        await Toon.SaveAsync(table, filePath);

        // Assert
        var content = File.ReadAllText(filePath);
        Assert.Contains("[2]{id,score,count}:", content);
        Assert.Contains("1,98.5,1000", content);
        Assert.Contains("2,87.5,2000", content);
    }

    // =========================================================================
    // Overwrite behaviour
    // =========================================================================

    [Fact]
    public async Task SaveAsync_OverwritesExistingFile()
    {
        // Arrange
        var filePath = GetTempFilePath();
        File.WriteAllText(filePath, "old content that should be replaced");

        var table = BuildSimpleTable();

        // Act
        await Toon.SaveAsync(table, filePath);

        // Assert
        var content = File.ReadAllText(filePath);
        Assert.DoesNotContain("old content", content);
        Assert.Contains("[2]{id,name,role}:", content);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static DataTable BuildSimpleTable()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("role", typeof(string));
        table.Rows.Add(1, "Alice", "admin");
        table.Rows.Add(2, "Bob", "user");
        return table;
    }
}
#endif
