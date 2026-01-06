using System.Text.Json;

namespace ToonFormat.Tests;

/// <summary>
/// Comprehensive tests for ToonDecoder to improve code coverage.
/// </summary>
public class ToonDecoderTests
{
    [Fact]
    public void Decode_EmptyInput_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Toon.Decode(""));
        Assert.Throws<ArgumentException>(() => Toon.Decode(null!));
    }

    [Fact]
    public void Decode_SinglePrimitiveValue_ReturnsCorrectElement()
    {
        // Arrange & Act
        var result = Toon.Decode("42");

        // Assert
        Assert.Equal(JsonValueKind.Number, result.ValueKind);
        Assert.Equal(42, result.GetInt32());
    }

    [Fact]
    public void Decode_SingleStringValue_ReturnsCorrectElement()
    {
        // Arrange & Act
        var result = Toon.Decode("\"hello\"");

        // Assert
        Assert.Equal(JsonValueKind.String, result.ValueKind);
        Assert.Equal("hello", result.GetString());
    }

    [Fact]
    public void Decode_SingleBooleanTrue_ReturnsCorrectElement()
    {
        // Arrange & Act
        var result = Toon.Decode("true");

        // Assert
        Assert.Equal(JsonValueKind.True, result.ValueKind);
        Assert.True(result.GetBoolean());
    }

    [Fact]
    public void Decode_SingleBooleanFalse_ReturnsCorrectElement()
    {
        // Arrange & Act
        var result = Toon.Decode("false");

        // Assert
        Assert.Equal(JsonValueKind.False, result.ValueKind);
        Assert.False(result.GetBoolean());
    }

    [Fact]
    public void Decode_SingleNull_ReturnsCorrectElement()
    {
        // Arrange & Act
        var result = Toon.Decode("null");

        // Assert
        Assert.Equal(JsonValueKind.Null, result.ValueKind);
    }

    [Fact]
    public void Decode_SimpleObject_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = "name: Alice\nage: 30";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
        Assert.Equal("Alice", result.GetProperty("name").GetString());
        Assert.Equal(30, result.GetProperty("age").GetInt32());
    }

    [Fact]
    public void Decode_ObjectWithNullValue_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = "name: Alice\nvalue: null";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        Assert.Equal(JsonValueKind.Null, result.GetProperty("value").ValueKind);
    }

    [Fact]
    public void Decode_ObjectWithQuotedKey_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = "\"first name\": Alice\nage: 30";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        Assert.Equal("Alice", result.GetProperty("first name").GetString());
        Assert.Equal(30, result.GetProperty("age").GetInt32());
    }

    [Fact]
    public void Decode_NestedObject_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = "person:\n  name: Alice\n  age: 30";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var person = result.GetProperty("person");
        Assert.Equal("Alice", person.GetProperty("name").GetString());
        Assert.Equal(30, person.GetProperty("age").GetInt32());
    }

    [Fact]
    public void Decode_EmptyObject_ReturnsEmptyObject()
    {
        // Arrange
        var toon = "person:";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var person = result.GetProperty("person");
        Assert.Equal(JsonValueKind.Object, person.ValueKind);
        Assert.Equal(0, person.EnumerateObject().Count());
    }

    [Fact]
    public void Decode_EmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        var toon = "items[0]:";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var items = result.GetProperty("items");
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.Equal(0, items.GetArrayLength());
    }

    [Fact]
    public void Decode_RootArray_ReturnsArray()
    {
        // Arrange
        var toon = "[3]: 1,2,3";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal(3, result.GetArrayLength());
        Assert.Equal(1, result[0].GetInt32());
        Assert.Equal(2, result[1].GetInt32());
        Assert.Equal(3, result[2].GetInt32());
    }

    [Fact]
    public void Decode_InlineArray_ReturnsCorrectArray()
    {
        // Arrange
        var toon = "numbers[5]: 1,2,3,4,5";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var numbers = result.GetProperty("numbers");
        Assert.Equal(5, numbers.GetArrayLength());
        Assert.Equal(1, numbers[0].GetInt32());
        Assert.Equal(5, numbers[4].GetInt32());
    }

    [Fact]
    public void Decode_TabularArray_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = "users[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var users = result.GetProperty("users");
        Assert.Equal(2, users.GetArrayLength());
        Assert.Equal(1, users[0].GetProperty("id").GetInt32());
        Assert.Equal("Alice", users[0].GetProperty("name").GetString());
        Assert.Equal("admin", users[0].GetProperty("role").GetString());
        Assert.Equal(2, users[1].GetProperty("id").GetInt32());
        Assert.Equal("Bob", users[1].GetProperty("name").GetString());
        Assert.Equal("user", users[1].GetProperty("role").GetString());
    }

    [Fact]
    public void Decode_TabularArrayWithPipeDelimiter_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = "users[2|]{id,name}:\n  1|Alice\n  2|Bob";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var users = result.GetProperty("users");
        Assert.Equal(2, users.GetArrayLength());
        Assert.Equal("Alice", users[0].GetProperty("name").GetString());
        Assert.Equal("Bob", users[1].GetProperty("name").GetString());
    }

    [Fact]
    public void Decode_TabularArrayWithTabDelimiter_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = "users[2\t]{id,name}:\n  1\tAlice\n  2\tBob";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var users = result.GetProperty("users");
        Assert.Equal(2, users.GetArrayLength());
        Assert.Equal("Alice", users[0].GetProperty("name").GetString());
    }

    [Fact]
    public void Decode_TabularArrayWithLengthMarker_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = "users[#2]{id,name}:\n  1,Alice\n  2,Bob";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var users = result.GetProperty("users");
        Assert.Equal(2, users.GetArrayLength());
    }

    [Fact]
    public void Decode_ListArray_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = "items[3]:\n  - apple\n  - banana\n  - cherry";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var items = result.GetProperty("items");
        Assert.Equal(3, items.GetArrayLength());
        Assert.Equal("apple", items[0].GetString());
        Assert.Equal("banana", items[1].GetString());
        Assert.Equal("cherry", items[2].GetString());
    }

    [Fact]
    public void Decode_ListArrayWithNumbers_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = "numbers[3]:\n  - 10\n  - 20\n  - 30";

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
    public void Decode_ListArrayWithObjects_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = "people[2]:\n  - name: Alice\n    age: 30\n  - name: Bob\n    age: 25";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var people = result.GetProperty("people");
        Assert.Equal(2, people.GetArrayLength());
        Assert.Equal("Alice", people[0].GetProperty("name").GetString());
        Assert.Equal(30, people[0].GetProperty("age").GetInt32());
        Assert.Equal("Bob", people[1].GetProperty("name").GetString());
        Assert.Equal(25, people[1].GetProperty("age").GetInt32());
    }

    [Fact]
    public void Decode_ListArrayWithNestedArrays_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = "matrix[2]:\n  - [3]: 1,2,3\n  - [3]: 4,5,6";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var matrix = result.GetProperty("matrix");
        Assert.Equal(2, matrix.GetArrayLength());
        Assert.Equal(3, matrix[0].GetArrayLength());
        Assert.Equal(1, matrix[0][0].GetInt32());
        Assert.Equal(3, matrix[0][2].GetInt32());
        Assert.Equal(4, matrix[1][0].GetInt32());
        Assert.Equal(6, matrix[1][2].GetInt32());
    }

    [Fact]
    public void Decode_ComplexNestedStructure_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = @"company: TechCorp
departments[2]{id,name}:
  1,Engineering
  2,Sales
metadata:
  created: 2024-01-01
  active: true";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        Assert.Equal("TechCorp", result.GetProperty("company").GetString());
        var departments = result.GetProperty("departments");
        Assert.Equal(2, departments.GetArrayLength());
        Assert.Equal("Engineering", departments[0].GetProperty("name").GetString());
        var metadata = result.GetProperty("metadata");
        Assert.Equal("2024-01-01", metadata.GetProperty("created").GetString());
        Assert.True(metadata.GetProperty("active").GetBoolean());
    }

    [Fact]
    public void Decode_WithStrictMode_ValidatesArrayLengths()
    {
        // Arrange - Array declares 3 items but only has 2
        var toon = "items[3]: 1,2";
        var options = new DecodeOptions { Strict = true };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => Toon.Decode(toon, options));
        Assert.Contains("Expected 3", exception.Message);
    }

    [Fact]
    public void Decode_WithNonStrictMode_ToleratesMissingItems()
    {
        // Arrange - Array declares 3 items but only has 2
        var toon = "items[3]: 1,2";
        var options = new DecodeOptions { Strict = false };

        // Act
        var result = Toon.Decode(toon, options);

        // Assert - Should decode successfully with only 2 items
        var items = result.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
    }

    [Fact]
    public void Decode_WithCustomIndent_ReturnsCorrectStructure()
    {
        // Arrange - 4 spaces indent
        var toon = "person:\n    name: Alice\n    age: 30";
        var options = new DecodeOptions { Indent = 4 };

        // Act
        var result = Toon.Decode(toon, options);

        // Assert
        var person = result.GetProperty("person");
        Assert.Equal("Alice", person.GetProperty("name").GetString());
        Assert.Equal(30, person.GetProperty("age").GetInt32());
    }

    [Fact]
    public void Decode_QuotedStringWithEscapes_ReturnsUnescapedValue()
    {
        // Arrange
        var toon = "message: \"Hello\\nWorld\\t!\"";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        Assert.Equal("Hello\nWorld\t!", result.GetProperty("message").GetString());
    }

    [Fact]
    public void Decode_QuotedStringWithQuotes_ReturnsCorrectValue()
    {
        // Arrange
        var toon = "quote: \"She said \\\"hello\\\"\"";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        Assert.Equal("She said \"hello\"", result.GetProperty("quote").GetString());
    }

    [Fact]
    public void Decode_FloatingPointNumber_ReturnsCorrectValue()
    {
        // Arrange
        var toon = "value: 3.14159";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        Assert.Equal(3.14159, result.GetProperty("value").GetDouble(), 5);
    }

    [Fact]
    public void Decode_NegativeNumber_ReturnsCorrectValue()
    {
        // Arrange
        var toon = "temperature: -15";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        Assert.Equal(-15, result.GetProperty("temperature").GetInt32());
    }

    [Fact]
    public void Decode_LargeNumber_ReturnsCorrectValue()
    {
        // Arrange
        var toon = "bignum: 9223372036854775807";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        Assert.Equal(9223372036854775807L, result.GetProperty("bignum").GetInt64());
    }

    [Fact]
    public void Decode_ScientificNotation_ReturnsCorrectValue()
    {
        // Arrange
        var toon = "science: 1.23e-4";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        Assert.Equal(1.23e-4, result.GetProperty("science").GetDouble(), 10);
    }

    [Fact]
    public void Decode_TabularArrayWithQuotedFields_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = "data[2]{\"first name\",\"last name\"}:\n  Alice,Smith\n  Bob,Jones";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var data = result.GetProperty("data");
        Assert.Equal("Alice", data[0].GetProperty("first name").GetString());
        Assert.Equal("Smith", data[0].GetProperty("last name").GetString());
    }

    [Fact]
    public void Decode_TabularArrayWithQuotedValues_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = "data[2]{id,text}:\n  1,\"Hello, World\"\n  2,\"Goodbye, World\"";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var data = result.GetProperty("data");
        Assert.Equal("Hello, World", data[0].GetProperty("text").GetString());
        Assert.Equal("Goodbye, World", data[1].GetProperty("text").GetString());
    }

    [Fact]
    public void Decode_MultipleDepthLevels_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = @"level1:
  level2:
    level3:
      value: deep";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var level1 = result.GetProperty("level1");
        var level2 = level1.GetProperty("level2");
        var level3 = level2.GetProperty("level3");
        Assert.Equal("deep", level3.GetProperty("value").GetString());
    }

    [Fact]
    public void Decode_MixedArrayAndObject_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = @"config:
  enabled: true
  items[2]:
    - first
    - second
  name: test";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var config = result.GetProperty("config");
        Assert.True(config.GetProperty("enabled").GetBoolean());
        var items = config.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal("first", items[0].GetString());
        Assert.Equal("test", config.GetProperty("name").GetString());
    }

    [Theory]
    [InlineData("value: 123", 123)]
    [InlineData("value: 0", 0)]
    [InlineData("value: -456", -456)]
    public void Decode_VariousIntegerValues_ReturnsCorrectValue(string toon, int expected)
    {
        // Act
        var result = Toon.Decode(toon);

        // Assert
        Assert.Equal(expected, result.GetProperty("value").GetInt32());
    }

    [Theory]
    [InlineData("flag: true", true)]
    [InlineData("flag: false", false)]
    public void Decode_BooleanValues_ReturnsCorrectValue(string toon, bool expected)
    {
        // Act
        var result = Toon.Decode(toon);

        // Assert
        Assert.Equal(expected, result.GetProperty("flag").GetBoolean());
    }

    [Fact]
    public void Decode_ArrayWithMixedTypes_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = "mixed[4]: 1,\"text\",true,null";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        var mixed = result.GetProperty("mixed");
        Assert.Equal(4, mixed.GetArrayLength());
        Assert.Equal(1, mixed[0].GetInt32());
        Assert.Equal("text", mixed[1].GetString());
        Assert.True(mixed[2].GetBoolean());
        Assert.Equal(JsonValueKind.Null, mixed[3].ValueKind);
    }

    [Fact]
    public void Decode_ListArrayWithoutListMarkers_ReturnsCorrectStructure()
    {
        // Arrange - Non-standard but should handle gracefully in non-strict mode
        var toon = "items[2]:\n  apple\n  banana";
        var options = new DecodeOptions { Strict = false };

        // Act
        var result = Toon.Decode(toon, options);

        // Assert
        var items = result.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
    }

    [Fact]
    public void Decode_RootTabularArray_ReturnsCorrectStructure()
    {
        // Arrange
        var toon = "[2]{id,name}:\n  1,Alice\n  2,Bob";

        // Act
        var result = Toon.Decode(toon);

        // Assert
        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal(2, result.GetArrayLength());
        Assert.Equal("Alice", result[0].GetProperty("name").GetString());
    }

    [Fact]
    public void Decode_EmptyString_ThrowsInvalidOperationException()
    {
        // Arrange
        var toon = "   ";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Toon.Decode(toon));
    }
}
