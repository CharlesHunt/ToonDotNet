using System.Text.Json;

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
        
        // Create a path that's invalid on all platforms:
        // 1. Use a directory that doesn't exist (no permissions to create it)
        // 2. Or use a path that's too long
        // 3. Or use a null/empty path (but that's tested elsewhere)
        
        // Option 1: Non-existent directory that we can't create
        var invalidPath = Path.Combine("/nonexistent_root_dir_that_should_never_exist_12345", "test.toon");
        
        // Act & Assert
        Assert.Throws<System.IO.DirectoryNotFoundException>(() => Toon.Save(testData, invalidPath));
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
    public void JSonLoad_WithCustomDecodeOptions_UsesOptions()
    {
        // Arrange
        var toonContent = "value: 42";
        var tempFile = CreateTempFile(toonContent);
        var decodeOptions = new DecodeOptions { Indent = 4, Strict = true };

        try
        {
            // Act
            JsonElement result = Toon.Load(tempFile, decodeOptions);

            // Assert
            Assert.Equal(JsonValueKind.Object, result.ValueKind);
            Assert.True(result.TryGetProperty("value", out var value));
            Assert.Equal(42, value.GetInt32());
        }
        finally
        {
            File.Delete(tempFile);
        }
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
    public void JsonLoad_EmptyFile_ThrowsArgumentException()
    {
        // Arrange
        var tempFile = CreateTempFile(string.Empty);

        try
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => Toon.Load(tempFile));
            Assert.Contains("Input cannot be null or empty", exception.Message);
        }
        finally
        {
            File.Delete(tempFile);
        }
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

    private string CreateTempFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content);
        return tempFile;
    }

    [Fact]
    public void JsonLoad_ValidToonFile_ReturnsJsonElement()
    {
        // Arrange
        var toonContent = "users[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user";
        var tempFile = CreateTempFile(toonContent);

        try
        {
            // Act
            JsonElement result = Toon.Load(tempFile);

            // Assert
            Assert.Equal(JsonValueKind.Object, result.ValueKind);
            Assert.True(result.TryGetProperty("users", out var users));
            Assert.Equal(JsonValueKind.Array, users.ValueKind);
            Assert.Equal(2, users.GetArrayLength());
            Assert.Equal("Alice", users[0].GetProperty("name").GetString());
            Assert.Equal("admin", users[0].GetProperty("role").GetString());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void JsonLoad_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.toon");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => Toon.Load(nonExistentFile));
    }

    [Fact]
    public void JsonLoad_InvalidToonFormat_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidContent = "users[10]{id:i,name,email,active:b}:\r\n  1,Alice,,,, Johnson,alice@example.com,true\r\n  2,Bob Smith,bob@example.com,true\r\n  3,Carol White,carol@example.com,false";
        var tempFile = CreateTempFile(invalidContent);

        try
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => Toon.Load(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void JsonLoad_ComplexNestedStructure_ReturnsCorrectJsonElement()
    {
        // Arrange
        var complexData = new
        {
            company = "TechCorp",
            departments = new[]
            {
                new { id = 1, name = "Engineering", headcount = 50 },
                new { id = 2, name = "Sales", headcount = 30 }
            }
        };
        var toonContent = Toon.Encode(complexData);
        var tempFile = CreateTempFile(toonContent);

        try
        {
            // Act
            JsonElement result = Toon.Load(tempFile);

            // Assert
            Assert.Equal("TechCorp", result.GetProperty("company").GetString());
            var departments = result.GetProperty("departments");
            Assert.Equal(2, departments.GetArrayLength());
            Assert.Equal("Engineering", departments[0].GetProperty("name").GetString());
            Assert.Equal(50, departments[0].GetProperty("headcount").GetInt32());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void JsonLoad_WithNullDecodeOptions_UsesDefaultOptions()
    {
        // Arrange
        var toonContent = "status: active";
        var tempFile = CreateTempFile(toonContent);

        try
        {
            // Act
            JsonElement result = Toon.Load(tempFile, null);

            // Assert
            Assert.Equal(JsonValueKind.Object, result.ValueKind);
            Assert.Equal("active", result.GetProperty("status").GetString());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void JsonLoad_SimplePrimitiveValue_ReturnsJsonElement()
    {
        // Arrange
        var toonContent = "count: 123";
        var tempFile = CreateTempFile(toonContent);

        try
        {
            // Act
            JsonElement result = Toon.Load(tempFile);

            // Assert
            Assert.Equal(123, result.GetProperty("count").GetInt32());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void JsonLoad_RoundTripConsistency_PreservesData()
    {
        // Arrange
        var originalData = new
        {
            id = 42,
            name = "Test User",
            active = true,
            score = 98.5
        };
        var toonContent = Toon.Encode(originalData);
        var tempFile = CreateTempFile(toonContent);

        try
        {
            // Act
            JsonElement result = Toon.Load(tempFile);

            // Assert
            Assert.Equal(42, result.GetProperty("id").GetInt32());
            Assert.Equal("Test User", result.GetProperty("name").GetString());
            Assert.True(result.GetProperty("active").GetBoolean());
            Assert.Equal(98.5, result.GetProperty("score").GetDouble());
        }
        finally
        {
            File.Delete(tempFile);
        }
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

    [Fact]
    public void Save_DirectoryDoesNotExist_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var testData = new { name = "Alice" };
        var pathInNonExistentDir = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString(),
            "test.toon"
        );

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => Toon.Save(testData, pathInNonExistentDir));
    }

    [Fact]
    public void Save_PathToNonExistentDirectory_ThrowsException()
    {
        // Arrange
        var testData = new { name = "Alice" };
        var invalidPath = Path.Combine(
            Path.GetTempPath(), 
            $"nonexistent_dir_{Guid.NewGuid()}", 
            "subfolder", 
            "test.toon"
        );

        // Act & Assert
        // This will fail because the directory doesn't exist and File.WriteAllText doesn't create it
        Assert.ThrowsAny<DirectoryNotFoundException>(() => Toon.Save(testData, invalidPath));
    }
}