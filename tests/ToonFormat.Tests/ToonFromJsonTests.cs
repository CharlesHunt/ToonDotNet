using System.Text.Json;

namespace ToonFormat.Tests;

/// <summary>
/// Tests for JSON-to-TOON conversion methods.
/// </summary>
public class ToonFromJsonTests
{
    [Fact]
    public void FromJson_SimpleObject_ReturnsCorrectToon()
    {
        // Arrange
        var json = "{\"name\":\"Alice\",\"age\":30,\"isActive\":true}";

        // Act
        string toon = Toon.FromJson(json);

        // Assert
        Assert.Contains("name: Alice", toon);
        Assert.Contains("age: 30", toon);
        Assert.Contains("isActive: true", toon);
    }

    [Fact]
    public void FromJson_TabularData_ProducesCompactToonFormat()
    {
        // Arrange
        var json = "{\"users\":[{\"id\":1,\"name\":\"Alice\",\"role\":\"admin\"},{\"id\":2,\"name\":\"Bob\",\"role\":\"user\"}]}";

        // Act
        string toon = Toon.FromJson(json);

        // Assert
        Assert.Contains("users[2]{id,name,role}:", toon);
        Assert.Contains("1,Alice,admin", toon);
        Assert.Contains("2,Bob,user", toon);
    }

    [Fact]
    public void FromJson_WithCustomOptions_UsesSpecifiedOptions()
    {
        // Arrange
        var json = "[{\"id\":1,\"name\":\"Alice\"},{\"id\":2,\"name\":\"Bob\"}]";
        var options = new EncodeOptions
        {
            Delimiter = '|',
            LengthMarker = '#',
            Indent = 4
        };

        // Act
        string toon = Toon.FromJson(json, options);

        // Assert
        Assert.Contains("[#2]", toon);
        Assert.Contains("|", toon);
    }

    [Fact]
    public void FromJson_NullOrEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Toon.FromJson(null!));
        Assert.Throws<ArgumentException>(() => Toon.FromJson(string.Empty));
        Assert.Throws<JsonException>(() => Toon.FromJson("   "));
    }

    [Fact]
    public void FromJson_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var invalidJson = "{invalid json}";

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => Toon.FromJson(invalidJson));
        Assert.Contains("Invalid JSON string", exception.Message);
    }

    [Fact]
    public void FromJson_ComplexNestedStructure_ConvertsCorrectly()
    {
        // Arrange
        var json = @"{
            ""company"":""TechCorp"",
            ""departments"":[
                {""id"":1,""name"":""Engineering"",""headcount"":50},
                {""id"":2,""name"":""Sales"",""headcount"":30}
            ]
        }";

        // Act
        string toon = Toon.FromJson(json);

        // Assert
        Assert.Contains("company: TechCorp", toon);
        Assert.Contains("departments[2]{id,name,headcount}:", toon);
        Assert.Contains("1,Engineering,50", toon);
        Assert.Contains("2,Sales,30", toon);
    }

    [Fact]
    public void FromJson_PrimitiveArray_ConvertsToInlineFormat()
    {
        // Arrange
        var json = "[1,2,3,4,5]";

        // Act
        string toon = Toon.FromJson(json);

        // Assert
        Assert.Contains("1,2,3,4,5", toon);
    }

    [Fact]
    public void FromJson_Null_ConvertsToNullValue()
    {
        // Arrange
        var json = "{\"value\":null}";

        // Act
        string toon = Toon.FromJson(json);

        // Assert
        Assert.Contains("value: null", toon);
    }

    [Fact]
    public void FromJson_RoundTrip_PreservesData()
    {
        // Arrange
        var json = "{\"id\":42,\"name\":\"Test\",\"score\":98.5,\"active\":true}";

        // Act
        string toon = Toon.FromJson(json);
        JsonElement decoded = Toon.Decode(toon);

        // Assert
        Assert.Equal(42, decoded.GetProperty("id").GetInt32());
        Assert.Equal("Test", decoded.GetProperty("name").GetString());
        Assert.Equal(98.5, decoded.GetProperty("score").GetDouble());
        Assert.True(decoded.GetProperty("active").GetBoolean());
    }

    [Fact]
    public void FromJson_ComparedToEncode_ProducesSameOutput()
    {
        // Arrange
        var obj = new
        {
            users = new[]
            {
                new { id = 1, name = "Alice", role = "admin" },
                new { id = 2, name = "Bob", role = "user" }
            }
        };
        var json = JsonSerializer.Serialize(obj);

        // Act
        string toonFromJson = Toon.FromJson(json);
        string toonFromObject = Toon.Encode(obj);

        // Assert - Both methods should produce identical output
        Assert.Equal(toonFromObject, toonFromJson);
    }

    [Fact]
    public void FromJson_WithNullOptions_UsesDefaultOptions()
    {
        // Arrange
        var json = "{\"value\":123}";

        // Act
        string toon = Toon.FromJson(json, null);

        // Assert
        Assert.Contains("value: 123", toon);
    }

    [Fact]
    public void FromJson_EmptyObject_ReturnsEmptyResult()
    {
        // Arrange
        var json = "{}";

        // Act
        string toon = Toon.FromJson(json);

        // Assert
        Assert.NotNull(toon);
    }

    [Fact]
    public void FromJson_EmptyArray_ReturnsEmptyArrayHeader()
    {
        // Arrange
        var json = "[]";

        // Act
        string toon = Toon.FromJson(json);

        // Assert
        Assert.Contains("[0]", toon);
    }

    [Fact]
    public void FromJsonFile_ValidJsonFile_ReturnsCorrectToon()
    {
        // Arrange
        var json = "{\"name\":\"Alice\",\"age\":30}";
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, json);

        try
        {
            // Act
            string toon = Toon.FromJsonFile(tempFile);

            // Assert
            Assert.Contains("name: Alice", toon);
            Assert.Contains("age: 30", toon);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void FromJsonFile_WithCustomOptions_UsesSpecifiedOptions()
    {
        // Arrange
        var json = "[{\"id\":1},{\"id\":2}]";
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, json);
        var options = new EncodeOptions { Delimiter = '|', LengthMarker = '#' };

        try
        {
            // Act
            string toon = Toon.FromJsonFile(tempFile, options);

            // Assert
            Assert.Contains("[#2]", toon);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void FromJsonFile_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.json");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => Toon.FromJsonFile(nonExistentFile));
    }

    [Fact]
    public void FromJsonFile_NullOrEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Toon.FromJsonFile(null!));
        Assert.Throws<ArgumentException>(() => Toon.FromJsonFile(string.Empty));
    }

    [Fact]
    public void FromJsonFile_InvalidJsonContent_ThrowsJsonException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "invalid json content");

        try
        {
            // Act & Assert
            Assert.Throws<JsonException>(() => Toon.FromJsonFile(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void FromJsonFile_ComplexData_RoundTripSuccessful()
    {
        // Arrange
        var data = new
        {
            company = "TechCorp",
            employees = new[]
            {
                new { id = 1, name = "Alice", department = "Engineering" },
                new { id = 2, name = "Bob", department = "Sales" }
            }
        };
        var json = JsonSerializer.Serialize(data);
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, json);

        try
        {
            // Act
            string toon = Toon.FromJsonFile(tempFile);
            JsonElement decoded = Toon.Decode(toon);

            // Assert
            Assert.Equal("TechCorp", decoded.GetProperty("company").GetString());
            var employees = decoded.GetProperty("employees");
            Assert.Equal(2, employees.GetArrayLength());
            Assert.Equal("Alice", employees[0].GetProperty("name").GetString());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData("{\"value\":123}", "value: 123")]
    [InlineData("{\"text\":\"hello\"}", "text: hello")]
    [InlineData("{\"flag\":true}", "flag: true")]
    [InlineData("{\"flag\":false}", "flag: false")]
    public void FromJson_VariousPrimitiveTypes_ConvertsCorrectly(string json, string expectedSubstring)
    {
        // Act
        string toon = Toon.FromJson(json);

        // Assert
        Assert.Contains(expectedSubstring, toon);
    }

    [Fact]
    public void FromJson_PerformanceTest_EfficientConversion()
    {
        // Arrange - Create a moderately sized JSON
        var data = Enumerable.Range(1, 100).Select(i => new
        {
            id = i,
            name = $"User{i}",
            email = $"user{i}@example.com",
            active = i % 2 == 0
        }).ToArray();
        var json = JsonSerializer.Serialize(data);

        // Act & Assert - Should complete quickly
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        string toon = Toon.FromJson(json);
        stopwatch.Stop();

        Assert.NotEmpty(toon);
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Conversion took {stopwatch.ElapsedMilliseconds}ms");
    }
}