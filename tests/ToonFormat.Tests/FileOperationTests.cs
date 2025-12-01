using System.Text.Json;
using ToonFormat.Decode;
using ToonFormat.Encode;

namespace ToonFormat.Tests;

public class FileOperationTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    private string GetTempFilePath()
    {
        var path = Path.Combine(Path.GetTempPath(), $"toon_test_{Guid.NewGuid()}.toon");
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        // Clean up temporary files
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    [Fact]
    public void Save_SimpleObject_CreatesFile()
    {
        // Arrange
        var testData = new { name = "Alice", age = 30, role = "admin" };
        var filePath = GetTempFilePath();

        // Act
        Toon.Save(testData, filePath);

        // Assert
        Assert.True(File.Exists(filePath));
        var content = File.ReadAllText(filePath);
        Assert.NotEmpty(content);
    }

    [Fact]
    public void Save_WithCustomEncodeOptions_UsesOptions()
    {
        // Arrange
        var testData = new { users = new[] { 
            new { id = 1, name = "Alice" },
            new { id = 2, name = "Bob" }
        }};
        var filePath = GetTempFilePath();
        var options = new EncodeOptions { Indent = 4, Delimiter = ';' };

        // Act
        Toon.Save(testData, filePath, options);

        // Assert
        Assert.True(File.Exists(filePath));
        var content = File.ReadAllText(filePath);
        Assert.Contains(";", content); // Verify delimiter is used
    }

    [Fact]
    public void Save_NullInput_CreatesFileWithEmptyContent()
    {
        // Arrange
        var filePath = GetTempFilePath();

        // Act
        Toon.Save(null, filePath);

        // Assert
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Save_InvalidFilePath_ThrowsException()
    {
        // Arrange
        var testData = new { name = "Alice" };
        var invalidPath = Path.Combine(Path.GetTempPath(), new string(Path.GetInvalidPathChars()[0], 1), "test.toon");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Toon.Save(testData, invalidPath));
    }

    [Fact]
    public void Load_ExistingFile_ReturnsDeserializedObject()
    {
        // Arrange
        var originalData = new TestUser { Id = 1, Name = "Alice", Role = "admin" };
        var filePath = GetTempFilePath();
        Toon.Save(originalData, filePath);

        // Act
        var loadedData = Toon.Load<TestUser>(filePath);

        // Assert
        Assert.NotNull(loadedData);
        Assert.Equal(originalData.Id, loadedData.Id);
        Assert.Equal(originalData.Name, loadedData.Name);
        Assert.Equal(originalData.Role, loadedData.Role);
    }

    [Fact]
    public void Load_WithCustomDecodeOptions_UsesOptions()
    {
        // Arrange
        var originalData = new { values = new[] { 1, 2, 3, 4, 5 } };
        var filePath = GetTempFilePath();
        var encodeOptions = new EncodeOptions { Indent = 4 };
        Toon.Save(originalData, filePath, encodeOptions);

        var decodeOptions = new DecodeOptions { Indent = 4, Strict = true };

        // Act
        var loadedData = Toon.Load<Dictionary<string, int[]>>(filePath, decodeOptions);

        // Assert
        Assert.NotNull(loadedData);
        Assert.Equal(5, loadedData["values"].Length);
    }

    [Fact]
    public void Load_WithCustomJsonOptions_UsesOptions()
    {
        // Arrange
        var originalData = new { UserName = "Alice", UserRole = "Admin" };
        var filePath = GetTempFilePath();
        Toon.Save(originalData, filePath);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var loadedData = Toon.Load<Dictionary<string, string>>(filePath, null, jsonOptions);

        // Assert
        Assert.NotNull(loadedData);
        Assert.Contains("userName", loadedData.Keys);
        Assert.Equal("Alice", loadedData["userName"]);
    }

    [Fact]
    public void Load_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.toon");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => Toon.Load<TestUser>(nonExistentPath));
    }

    [Fact]
    public void Load_InvalidToonContent_ThrowsException()
    {
        // Arrange
        var filePath = GetTempFilePath();
        File.WriteAllText(filePath, "invalid [[ toon syntax");

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => Toon.Load<TestUser>(filePath));
    }

    [Fact]
    public void Load_EmptyFile_ThrowsArgumentException()
    {
        // Arrange
        var filePath = GetTempFilePath();
        File.WriteAllText(filePath, string.Empty);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Toon.Load<TestUser>(filePath));
    }

    [Fact]
    public void SaveAndLoad_ComplexObject_MaintainsDataFidelity()
    {
        // Arrange
        var originalData = new ComplexTestData
        {
            Id = 42,
            Name = "Test Project",
            Tags = new[] { "tag1", "tag2", "tag3" },
            Settings = new Dictionary<string, object>
            {
                { "enabled", true },
                { "threshold", 99.5 }
            },
            Users = new[]
            {
                new TestUser { Id = 1, Name = "Alice", Role = "admin" },
                new TestUser { Id = 2, Name = "Bob", Role = "user" }
            }
        };
        var filePath = GetTempFilePath();

        // Act
        Toon.Save(originalData, filePath);
        var loadedData = Toon.Load<ComplexTestData>(filePath);

        // Assert
        Assert.NotNull(loadedData);
        Assert.Equal(originalData.Id, loadedData.Id);
        Assert.Equal(originalData.Name, loadedData.Name);
        Assert.Equal(originalData.Tags.Length, loadedData.Tags.Length);
        Assert.Equal(2, loadedData.Users.Length);
        Assert.Equal("Alice", loadedData.Users[0].Name);
        Assert.Equal("Bob", loadedData.Users[1].Name);
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_PreservesAllData()
    {
        // Arrange
        var originalData = new TestUser { Id = 100, Name = "Charlie", Role = "moderator" };
        var filePath = GetTempFilePath();

        // Act - First round trip
        Toon.Save(originalData, filePath);
        var firstLoad = Toon.Load<TestUser>(filePath);

        // Act - Second round trip
        var secondFilePath = GetTempFilePath();
        Toon.Save(firstLoad, secondFilePath);
        var secondLoad = Toon.Load<TestUser>(secondFilePath);

        // Assert
        Assert.Equal(originalData.Id, secondLoad.Id);
        Assert.Equal(originalData.Name, secondLoad.Name);
        Assert.Equal(originalData.Role, secondLoad.Role);
    }

    [Fact]
    public void SaveAndLoad_ArrayOfPrimitives_WorksCorrectly()
    {
        // Arrange
        var originalData = new { numbers = new[] { 1, 2, 3, 4, 5 }, strings = new[] { "a", "b", "c" } };
        var filePath = GetTempFilePath();

        // Act
        Toon.Save(originalData, filePath);
        var loadedData = Toon.Load<Dictionary<string, JsonElement>>(filePath);

        // Assert
        Assert.NotNull(loadedData);
        var numbers = loadedData["numbers"].EnumerateArray().Select(e => e.GetInt32()).ToArray();
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, numbers);
    }

    // Helper classes for testing
    private class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    private class ComplexTestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string[] Tags { get; set; } = Array.Empty<string>();
        public Dictionary<string, object> Settings { get; set; } = new();
        public TestUser[] Users { get; set; } = Array.Empty<TestUser>();
    }
}