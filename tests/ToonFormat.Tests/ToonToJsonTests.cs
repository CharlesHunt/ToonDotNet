using System.Text.Json;

namespace ToonFormat.Tests;

/// <summary>
/// Tests for TOON-to-JSON conversion methods.
/// </summary>
public class ToonToJsonTests
{
    [Fact]
    public void ToJson_SimpleObject_ReturnsCorrectJson()
    {
        // Arrange
        var toon = "name: Alice\nage: 30\nisActive: true";

        // Act
        string json = Toon.ToJson(toon);
        var jsonElement = JsonDocument.Parse(json).RootElement;

        // Assert
        Assert.Equal("Alice", jsonElement.GetProperty("name").GetString());
        Assert.Equal(30, jsonElement.GetProperty("age").GetInt32());
        Assert.True(jsonElement.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public void ToJson_TabularData_ReturnsCorrectJson()
    {
        // Arrange
        var toon = "users[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user";

        // Act
        string json = Toon.ToJson(toon);
        var jsonElement = JsonDocument.Parse(json).RootElement;

        // Assert
        var users = jsonElement.GetProperty("users");
        Assert.Equal(2, users.GetArrayLength());
        Assert.Equal("Alice", users[0].GetProperty("name").GetString());
        Assert.Equal("admin", users[0].GetProperty("role").GetString());
        Assert.Equal("Bob", users[1].GetProperty("name").GetString());
    }

    [Fact]
    public void ToJson_WithIndentedOutput_ProducesFormattedJson()
    {
        // Arrange
        var toon = "name: Alice\nage: 30";
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        // Act
        string json = Toon.ToJson(toon, null, jsonOptions);

        // Assert
        Assert.Contains("\n", json); // Indented JSON contains newlines
        Assert.Contains("  ", json); // Indented JSON contains spaces
    }

    [Fact]
    public void ToJson_WithCompactOutput_ProducesMinimalJson()
    {
        // Arrange
        var toon = "name: Alice\nage: 30";
        var jsonOptions = new JsonSerializerOptions { WriteIndented = false };

        // Act
        string json = Toon.ToJson(toon, null, jsonOptions);

        // Assert
        Assert.DoesNotContain("\n  ", json); // Compact JSON has no indentation
        Assert.Equal("{\"name\":\"Alice\",\"age\":30}", json);
    }

    [Fact]
    public void ToJson_NullOrEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Toon.ToJson(null!));
        Assert.Throws<ArgumentException>(() => Toon.ToJson(string.Empty));
        Assert.Throws<InvalidOperationException>(() => Toon.ToJson("   "));
    }

    [Fact]
    public void ToJson_InvalidToon_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidToon = "users[10]{id:i,name,email,active:b}:\r\n  1,Alice,,,, Johnson,alice@example.com,true\r\n  2,Bob Smith,bob@example.com,true\r\n  3,Carol White,carol@example.com,false";       

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Toon.ToJson(invalidToon));
    }

    [Fact]
    public void ToJson_ComplexNestedStructure_ConvertsCorrectly()
    {
        // Arrange
        var toon = "company: TechCorp\ndepartments[2]{id,name,headcount}:\n  1,Engineering,50\n  2,Sales,30";

        // Act
        string json = Toon.ToJson(toon);
        var jsonElement = JsonDocument.Parse(json).RootElement;

        // Assert
        Assert.Equal("TechCorp", jsonElement.GetProperty("company").GetString());
        var departments = jsonElement.GetProperty("departments");
        Assert.Equal(2, departments.GetArrayLength());
        Assert.Equal("Engineering", departments[0].GetProperty("name").GetString());
        Assert.Equal(50, departments[0].GetProperty("headcount").GetInt32());
    }

    [Fact]
    public void ToJson_PrimitiveArray_ConvertsCorrectly()
    {
        // Arrange
        var toon = "[5]: 1,2,3,4,5";

        // Act
        string json = Toon.ToJson(toon);
        var jsonElement = JsonDocument.Parse(json).RootElement;

        // Assert
        Assert.Equal(JsonValueKind.Array, jsonElement.ValueKind);
        Assert.Equal(5, jsonElement.GetArrayLength());
        Assert.Equal(1, jsonElement[0].GetInt32());
        Assert.Equal(5, jsonElement[4].GetInt32());
    }

    [Fact]
    public void ToJson_WithNullValue_ConvertsCorrectly()
    {
        // Arrange
        var toon = "value: null";

        // Act
        string json = Toon.ToJson(toon);
        var jsonElement = JsonDocument.Parse(json).RootElement;

        // Assert
        Assert.Equal(JsonValueKind.Null, jsonElement.GetProperty("value").ValueKind);
    }

    [Fact]
    public void ToJson_RoundTrip_PreservesData()
    {
        // Arrange
        var originalJson = "{\"id\":42,\"name\":\"Test\",\"score\":98.5,\"active\":true}";

        // Act - JSON -> TOON -> JSON
        string toon = Toon.FromJson(originalJson);
        string resultJson = Toon.ToJson(toon);
        var original = JsonDocument.Parse(originalJson).RootElement;
        var result = JsonDocument.Parse(resultJson).RootElement;

        // Assert
        Assert.Equal(original.GetProperty("id").GetInt32(), result.GetProperty("id").GetInt32());
        Assert.Equal(original.GetProperty("name").GetString(), result.GetProperty("name").GetString());
        Assert.Equal(original.GetProperty("score").GetDouble(), result.GetProperty("score").GetDouble());
        Assert.Equal(original.GetProperty("active").GetBoolean(), result.GetProperty("active").GetBoolean());
    }

    [Fact]
    public void ToJson_WithCustomDecodeOptions_UsesOptions()
    {
        // Arrange
        var toon = "value: 42";
        var decodeOptions = new DecodeOptions { Indent = 2, Strict = false };

        // Act
        string json = Toon.ToJson(toon, decodeOptions);
        var jsonElement = JsonDocument.Parse(json).RootElement;

        // Assert
        Assert.Equal(42, jsonElement.GetProperty("value").GetInt32());
    }

    [Fact]
    public void ToJson_WithNullOptions_UsesDefaults()
    {
        // Arrange
        var toon = "status: active";

        // Act
        string json = Toon.ToJson(toon, null, null);
        var jsonElement = JsonDocument.Parse(json).RootElement;

        // Assert
        Assert.Equal("active", jsonElement.GetProperty("status").GetString());
    }

    [Fact]
    public void ToJsonFile_ValidToonFile_ReturnsCorrectJson()
    {
        // Arrange
        var toon = "name: Alice\nage: 30";
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, toon);

        try
        {
            // Act
            string json = Toon.ToJsonFile(tempFile);
            var jsonElement = JsonDocument.Parse(json).RootElement;

            // Assert
            Assert.Equal("Alice", jsonElement.GetProperty("name").GetString());
            Assert.Equal(30, jsonElement.GetProperty("age").GetInt32());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ToJsonFile_WithCustomOptions_UsesSpecifiedOptions()
    {
        // Arrange
        var toon = "users[2]{id,name}:\n  1,Alice\n  2,Bob";
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, toon);
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        try
        {
            // Act
            string json = Toon.ToJsonFile(tempFile, null, jsonOptions);

            // Assert
            Assert.Contains("\n", json);
            Assert.Contains("\"users\"", json);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ToJsonFile_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.toon");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => Toon.ToJsonFile(nonExistentFile));
    }

    [Fact]
    public void ToJsonFile_NullOrEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Toon.ToJsonFile(null!));
        Assert.Throws<ArgumentException>(() => Toon.ToJsonFile(string.Empty));
    }

    [Fact]
    public void ToJsonFile_InvalidToonContent_ThrowsInvalidOperationException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var invalidContent = "users[10]{id:i,name,email,active:b}:\r\n  1,Alice,,,, Johnson,alice@example.com,true\r\n  2,Bob Smith,bob@example.com,true\r\n  3,Carol White,carol@example.com,false";
        File.WriteAllText(tempFile, invalidContent);

        try
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => Toon.ToJsonFile(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveAsJson_ValidToon_CreatesJsonFile()
    {
        // Arrange
        var toon = "name: Alice\nage: 30";
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            Toon.SaveAsJson(toon, tempFile);

            // Assert
            Assert.True(File.Exists(tempFile));
            var json = File.ReadAllText(tempFile);
            var jsonElement = JsonDocument.Parse(json).RootElement;
            Assert.Equal("Alice", jsonElement.GetProperty("name").GetString());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveAsJson_WithIndentedOption_CreatesFormattedJsonFile()
    {
        // Arrange
        var toon = "name: Alice\nage: 30";
        var tempFile = Path.GetTempFileName();
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        try
        {
            // Act
            Toon.SaveAsJson(toon, tempFile, null, jsonOptions);

            // Assert
            var json = File.ReadAllText(tempFile);
            Assert.Contains("\n", json);
            Assert.Contains("  \"name\"", json);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveAsJson_NullToonString_ThrowsArgumentException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Toon.SaveAsJson(null!, tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveAsJson_NullFilePath_ThrowsArgumentException()
    {
        // Arrange
        var toon = "name: Alice";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Toon.SaveAsJson(toon, null!));
    }

    [Fact]
    public void SaveAsJson_ComplexData_RoundTripSuccessful()
    {
        // Arrange
        var originalData = new
        {
            company = "TechCorp",
            employees = new[]
            {
                new { id = 1, name = "Alice", department = "Engineering" },
                new { id = 2, name = "Bob", department = "Sales" }
            }
        };
        var toon = Toon.Encode(originalData);
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            Toon.SaveAsJson(toon, tempFile);
            var json = File.ReadAllText(tempFile);
            var jsonElement = JsonDocument.Parse(json).RootElement;

            // Assert
            Assert.Equal("TechCorp", jsonElement.GetProperty("company").GetString());
            var employees = jsonElement.GetProperty("employees");
            Assert.Equal(2, employees.GetArrayLength());
            Assert.Equal("Alice", employees[0].GetProperty("name").GetString());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData("value: 123")]
    [InlineData("text: hello")]
    [InlineData("flag: true")]
    [InlineData("flag: false")]
    [InlineData("value: null")]
    public void ToJson_VariousPrimitiveTypes_ConvertsCorrectly(string toon)
    {
        // Act
        string json = Toon.ToJson(toon);

        // Assert
        Assert.NotEmpty(json);
        var jsonElement = JsonDocument.Parse(json).RootElement;
        Assert.Equal(JsonValueKind.Object, jsonElement.ValueKind);
    }

    [Fact]
    public void ToJson_EmptyObject_ReturnsEmptyJsonObject()
    {
        // Arrange - Create empty TOON array (encode will handle this)
        var data = Array.Empty<string>();
        var toon = Toon.Encode(data);

        // Act
        string json = Toon.ToJson(toon);

        // Assert
        Assert.Equal("[]", json);
    }

    [Fact]
    public void ToJson_PerformanceTest_EfficientConversion()
    {
        // Arrange - Create a moderately sized TOON
        var data = Enumerable.Range(1, 100).Select(i => new
        {
            id = i,
            name = $"User{i}",
            email = $"user{i}@example.com",
            active = i % 2 == 0
        }).ToArray();
        var toon = Toon.Encode(data);

        // Act & Assert - Should complete quickly
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        string json = Toon.ToJson(toon);
        stopwatch.Stop();

        Assert.NotEmpty(json);
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Conversion took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void ToJson_FullRoundTrip_JsonToToonToJson_PreservesData()
    {
        // Arrange
        var originalJson = "{\"users\":[{\"id\":1,\"name\":\"Alice\",\"role\":\"admin\"},{\"id\":2,\"name\":\"Bob\",\"role\":\"user\"}]}";

        // Act
        string toon = Toon.FromJson(originalJson);
        string resultJson = Toon.ToJson(toon);

        // Parse both to compare structure
        var original = JsonDocument.Parse(originalJson).RootElement;
        var result = JsonDocument.Parse(resultJson).RootElement;

        // Assert - Deep equality
        Assert.True(JsonElement.DeepEquals(original, result));
    }
}