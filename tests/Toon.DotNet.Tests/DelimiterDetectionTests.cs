using System.Text.Json;

namespace ToonFormat.Tests;

/// <summary>
/// Tests for delimiter detection during decoding.
/// These tests verify that the decoder correctly detects and uses delimiters
/// specified in the TOON format (pipe, tab, comma).
/// </summary>
public class DelimiterDetectionTests
{
    [Fact]
    public void Decode_TabularArrayWithPipeDelimiter_UsesDetectedDelimiter()
    {
        // Arrange - Pipe delimiter explicitly in bracket
        var toon = "users[2|]{id,name}:\n  1|Alice\n  2|Bob";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var users = result.GetProperty("users");
        Assert.Equal(2, users.GetArrayLength());
        Assert.Equal(1, users[0].GetProperty("id").GetInt32());
        Assert.Equal("Alice", users[0].GetProperty("name").GetString());
        Assert.Equal(2, users[1].GetProperty("id").GetInt32());
        Assert.Equal("Bob", users[1].GetProperty("name").GetString());
    }

    [Fact]
    public void Decode_InlineArrayWithPipeDelimiter_UsesDetectedDelimiter()
    {
        // Arrange
        var toon = "numbers[3|]: 10|20|30";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var numbers = result.GetProperty("numbers");
        Assert.Equal(3, numbers.GetArrayLength());
        Assert.Equal(10, numbers[0].GetInt32());
        Assert.Equal(20, numbers[1].GetInt32());
        Assert.Equal(30, numbers[2].GetInt32());
    }

    [Fact]
    public void Decode_NestedArraysWithDifferentDelimiters_UsesCorrectDelimiterForEach()
    {
        // Arrange - Outer array uses comma, inner arrays use pipe
        var toon = "matrix[2]:\n  - [3|]: 1|2|3\n  - [3|]: 4|5|6";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var matrix = result.GetProperty("matrix");
        Assert.Equal(2, matrix.GetArrayLength());
        Assert.Equal(3, matrix[0].GetArrayLength());
        Assert.Equal(1, matrix[0][0].GetInt32());
        Assert.Equal(2, matrix[0][1].GetInt32());
        Assert.Equal(3, matrix[0][2].GetInt32());
        Assert.Equal(4, matrix[1][0].GetInt32());
        Assert.Equal(5, matrix[1][1].GetInt32());
        Assert.Equal(6, matrix[1][2].GetInt32());
    }

    [Fact]
    public void Decode_TabularArrayWithCommaInValues_RequiresPipeDelimiter()
    {
        // Arrange - Values contain commas, so pipe delimiter is needed
        var toon = "addresses[2|]{id,address}:\n  1|123 Main St, Apt 4\n  2|456 Oak Ave, Suite 10";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var addresses = result.GetProperty("addresses");
        Assert.Equal(2, addresses.GetArrayLength());
        Assert.Equal("123 Main St, Apt 4", addresses[0].GetProperty("address").GetString());
        Assert.Equal("456 Oak Ave, Suite 10", addresses[1].GetProperty("address").GetString());
    }

    [Fact]
    public void Decode_MixedDelimitersInSameDocument_UsesCorrectDelimiterPerArray()
    {
        // Arrange - Different arrays with different delimiters in same document
        var toon = @"commaArray[3]: 1,2,3
pipeArray[3|]: a|b|c
tabArray[3	]: x	y	z";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var commaArray = result.GetProperty("commaArray");
        Assert.Equal(3, commaArray.GetArrayLength());
        Assert.Equal(1, commaArray[0].GetInt32());

        var pipeArray = result.GetProperty("pipeArray");
        Assert.Equal(3, pipeArray.GetArrayLength());
        Assert.Equal("a", pipeArray[0].GetString());

        var tabArray = result.GetProperty("tabArray");
        Assert.Equal(3, tabArray.GetArrayLength());
        Assert.Equal("x", tabArray[0].GetString());
    }

    [Fact]
    public void Decode_RootArrayWithPipeDelimiter_UsesDetectedDelimiter()
    {
        // Arrange
        var toon = "[4|]: apple|banana|cherry|date";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal(4, result.GetArrayLength());
        Assert.Equal("apple", result[0].GetString());
        Assert.Equal("banana", result[1].GetString());
        Assert.Equal("cherry", result[2].GetString());
        Assert.Equal("date", result[3].GetString());
    }

    [Fact]
    public void Decode_TabularArrayWithPipeAndQuotedValues_ParsesCorrectly()
    {
        // Arrange - Pipe delimiter with quoted values that contain pipes
        var toon = "data[2|]{id,description}:\n  1|\"contains | pipe\"\n  2|\"another | value\"";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var data = result.GetProperty("data");
        Assert.Equal(2, data.GetArrayLength());
        Assert.Equal("contains | pipe", data[0].GetProperty("description").GetString());
        Assert.Equal("another | value", data[1].GetProperty("description").GetString());
    }

    [Fact]
    public void Encode_ThenDecode_PreservesDelimiter()
    {
        // Arrange - Encode with pipe delimiter
        var originalData = new { items = new[] { "a", "b", "c" } };
        var encodeOptions = new EncodeOptions { Delimiter = '|' };

        // Act
        var toon = Toon.Encode(originalData, encodeOptions);
        var result = Toon.Decode(toon);

        // Assert - Should decode correctly with auto-detected pipe delimiter
        var items = result.GetProperty("items");
        Assert.Equal(3, items.GetArrayLength());
        Assert.Equal("a", items[0].GetString());
        Assert.Equal("b", items[1].GetString());
        Assert.Equal("c", items[2].GetString());
    }

    [Fact]
    public void Decode_ComplexNestedWithPipeDelimiter_ParsesAllLevels()
    {
        // Arrange
        var toon = @"config:
  settings[2|]{key,value}:
    timeout|30
    maxRetries|5
  tags[3|]: prod|stable|v1";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var config = result.GetProperty("config");
        var settings = config.GetProperty("settings");
        Assert.Equal(2, settings.GetArrayLength());
        Assert.Equal("timeout", settings[0].GetProperty("key").GetString());
        Assert.Equal(30, settings[0].GetProperty("value").GetInt32());
        
        var tags = config.GetProperty("tags");
        Assert.Equal(3, tags.GetArrayLength());
        Assert.Equal("prod", tags[0].GetString());
        Assert.Equal("stable", tags[1].GetString());
        Assert.Equal("v1", tags[2].GetString());
    }
}
