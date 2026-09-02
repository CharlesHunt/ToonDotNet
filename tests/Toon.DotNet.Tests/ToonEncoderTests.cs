using System.Text.Json;

namespace ToonFormat.Tests;

/// <summary>
/// Comprehensive tests for ToonEncoder to improve code coverage.
/// Tests various encoding scenarios including primitives, objects, arrays, and edge cases.
/// </summary>
public class ToonEncoderTests
{
    [Fact]
    public void Encode_SinglePrimitive_ReturnsCorrectString()
    {
        // Arrange
        var data = 42;

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Equal("42", result);
    }

    [Fact]
    public void Encode_SingleString_ReturnsCorrectString()
    {
        // Arrange
        var data = "hello";

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Equal("hello", result);
    }

    [Fact]
    public void Encode_QuotedStringWithSpecialChars_ReturnsEscapedString()
    {
        // Arrange
        var data = new { text = "hello, world" };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("\"hello, world\"", result);
    }

    [Fact]
    public void Encode_BooleanTrue_ReturnsTrue()
    {
        // Arrange
        var data = true;

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Equal("true", result);
    }

    [Fact]
    public void Encode_BooleanFalse_ReturnsFalse()
    {
        // Arrange
        var data = false;

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Equal("false", result);
    }

    [Fact]
    public void Encode_Null_ReturnsNull()
    {
        // Arrange
        object? data = null;

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Equal("null", result);
    }

    [Fact]
    public void Encode_EmptyObject_ReturnsEmptyLines()
    {
        // Arrange
        var data = new { };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Encode_SimpleObject_ReturnsKeyValuePairs()
    {
        // Arrange
        var data = new { name = "Alice", age = 30 };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("name: Alice", result);
        Assert.Contains("age: 30", result);
    }

    [Fact]
    public void Encode_NestedObject_ReturnsIndentedStructure()
    {
        // Arrange
        var data = new { person = new { name = "Alice", age = 30 } };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("person:", result);
        Assert.Contains("  name: Alice", result);
        Assert.Contains("  age: 30", result);
    }

    [Fact]
    public void Encode_EmptyNestedObject_ReturnsKeyWithColon()
    {
        // Arrange
        var data = new { person = new { } };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Equal("person:", result);
    }

    [Fact]
    public void Encode_ObjectWithCustomIndent_UsesCustomIndentation()
    {
        // Arrange
        var data = new { person = new { name = "Alice" } };
        var options = new EncodeOptions { Indent = 4 };

        // Act
        var result = Toon.Encode(data, options);

        // Assert
        Assert.Contains("person:", result);
        Assert.Contains("    name: Alice", result);
    }

    [Fact]
    public void Encode_EmptyArray_ReturnsEmptyArrayHeader()
    {
        // Arrange
        var data = new { items = new int[0] };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("items[0]:", result);
    }

    [Fact]
    public void Encode_PrimitiveArray_ReturnsInlineFormat()
    {
        // Arrange
        var data = new { numbers = new[] { 1, 2, 3, 4, 5 } };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("numbers[5]: 1,2,3,4,5", result);
    }

    [Fact]
    public void Encode_StringArray_ReturnsInlineFormat()
    {
        // Arrange
        var data = new[] { "apple", "banana", "cherry" };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("[3]: apple,banana,cherry", result);
    }

    [Fact]
    public void Encode_ArrayWithPipeDelimiter_UsesPipeDelimiter()
    {
        // Arrange
        var data = new[] { "a", "b", "c" };
        var options = new EncodeOptions { Delimiter = '|' };

        // Act
        var result = Toon.Encode(data, options);

        // Assert
        Assert.Contains("[3|]: a|b|c", result);
    }

    [Fact]
    public void Encode_ArrayWithLengthMarker_IncludesLengthMarker()
    {
        // Arrange
        var data = new[] { 1, 2, 3 };
        var options = new EncodeOptions { LengthMarker = '#' };

        // Act
        var result = Toon.Encode(data, options);

        // Assert
        Assert.Contains("[#3]:", result);
    }

    [Fact]
    public void Encode_TabularArray_ReturnsTabularFormat()
    {
        // Arrange
        var data = new
        {
            users = new[]
            {
                new { id = 1, name = "Alice", role = "admin" },
                new { id = 2, name = "Bob", role = "user" }
            }
        };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("users[2]{id,name,role}:", result);
        Assert.Contains("1,Alice,admin", result);
        Assert.Contains("2,Bob,user", result);
    }

    [Fact]
    public void Encode_TabularArrayWithNullValue_IncludesNullInRow()
    {
        // Arrange
        var data = new[]
        {
            new { id = 1, name = "Alice", role = (string?)null },
            new { id = 2, name = "Bob", role = "user" }
        };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("1,Alice,null", result);
        Assert.Contains("2,Bob,user", result);
    }

    [Fact]
    public void Encode_TabularArrayWithMissingProperty_UsesListFormat()
    {
        // Arrange - Non-uniform: first missing 'role', second has it
        var json = "[{\"id\":1,\"name\":\"Alice\"},{\"id\":2,\"name\":\"Bob\",\"role\":\"user\"}]";
        var data = new { users = JsonSerializer.Deserialize<JsonElement>(json) };

        // Act
        var result = Toon.Encode(data);

        // Assert - Should use list format, not tabular
        Assert.Contains("users[2]:", result);
        Assert.Contains("- id: 1", result);
        Assert.Contains("name: Alice", result);
        Assert.Contains("- id: 2", result);
        Assert.Contains("name: Bob", result);
        Assert.Contains("role: user", result);
    }

    [Fact]
    public void Encode_ArrayOfArrays_ReturnsListFormat()
    {
        // Arrange
        var data = new
        {
            matrix = new[]
            {
                new[] { 1, 2, 3 },
                new[] { 4, 5, 6 }
            }
        };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("matrix[2]:", result);
        Assert.Contains("- [3]: 1,2,3", result);
        Assert.Contains("- [3]: 4,5,6", result);
    }

    [Fact]
    public void Encode_MixedArray_ReturnsListFormat()
    {
        // Arrange
        var data = new object[] { 1, "text", true, null };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("[4]:", result);
        Assert.Contains("1,text,true,null", result);
    }

    [Fact]
    public void Encode_ArrayOfObjects_ReturnsTabularIfUniform()
    {
        // Arrange
        var data = new[]
        {
            new { id = 1, name = "Alice" },
            new { id = 2, name = "Bob" }
        };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("[2]{id,name}:", result);
        Assert.Contains("1,Alice", result);
        Assert.Contains("2,Bob", result);
    }

    [Fact]
    public void Encode_ArrayOfObjectsNonUniform_ReturnsListFormat()
    {
        // Arrange - Objects with different structures
        var json = "[{\"id\":1,\"name\":\"Alice\"},{\"id\":2,\"email\":\"bob@test.com\"}]";
        var data = JsonSerializer.Deserialize<JsonElement>(json);

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("[2]:", result);
        Assert.Contains("- id: 1", result);
        Assert.Contains("- id: 2", result);
    }

    [Fact]
    public void Encode_ArrayOfObjectsWithNestedValues_ReturnsListFormat()
    {
        // Arrange - Objects with non-primitive values
        var data = new[]
        {
            new { id = 1, details = new { age = 30 } },
            new { id = 2, details = new { age = 25 } }
        };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("[2]:", result);
        Assert.Contains("- id: 1", result);
    }

    [Fact]
    public void Encode_ListItemWithPrimitiveArray_ReturnsInlineArray()
    {
        // Arrange
        var data = new[]
        {
            new { id = 1, tags = new[] { "a", "b", "c" } },
            new { id = 2, tags = new[] { "x", "y", "z" } }
        };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("tags[3]: a,b,c", result);
    }

    [Fact]
    public void Encode_ListItemWithEmptyObject_ReturnsEmptyMarker()
    {
        // Arrange
        var data = new[] { new { } };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("-", result);
    }

    [Fact]
    public void Encode_ListItemWithNestedObject_ReturnsNestedStructure()
    {
        // Arrange
        var data = new[]
        {
            new { id = 1, person = new { name = "Alice" } }
        };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("- id: 1", result);
        Assert.Contains("  person:", result);
        Assert.Contains("    name: Alice", result);
    }

    [Fact]
    public void Encode_ListItemWithEmptyNestedObject_ReturnsKeyWithColon()
    {
        // Arrange
        var data = new[]
        {
            new { id = 1, data = new { } }
        };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("- id: 1", result);
        Assert.Contains("  data:", result);
    }

    [Fact]
    public void Encode_ListItemWithTabularArray_ReturnsTabularFormat()
    {
        // Arrange
        var data = new[]
        {
            new
            {
                id = 1,
                items = new[]
                {
                    new { x = 1, y = 2 },
                    new { x = 3, y = 4 }
                }
            }
        };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("items[2]{x,y}:", result);
        Assert.Contains("  1,2", result);
        Assert.Contains("  3,4", result);
    }

    [Fact]
    public void Encode_ListItemWithNonUniformArrayOfObjects_ReturnsListFormat()
    {
        // Arrange
        var json = "[{\"id\":1,\"items\":[{\"a\":1},{\"b\":2}]}]";
        var data = JsonSerializer.Deserialize<JsonElement>(json);

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("- id: 1", result);
        Assert.Contains("  items[2]:", result);
    }

    [Fact]
    public void Encode_ListItemWithComplexArray_ReturnsNestedListFormat()
    {
        // Arrange
        var data = new[]
        {
            new
            {
                id = 1,
                items = new object[] { 1, "text", new { x = 5 } }
            }
        };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("- id: 1", result);
        Assert.Contains("  items[3]:", result);
        Assert.Contains("  - 1", result);
        Assert.Contains("  - text", result);
    }

    [Fact]
    public void Encode_DeepNesting_ReturnsCorrectIndentation()
    {
        // Arrange
        var data = new
        {
            level1 = new
            {
                level2 = new
                {
                    level3 = new
                    {
                        value = "deep"
                    }
                }
            }
        };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("level1:", result);
        Assert.Contains("  level2:", result);
        Assert.Contains("    level3:", result);
        Assert.Contains("      value: deep", result);
    }

    [Fact]
    public void Encode_ComplexMixedStructure_ReturnsCorrectFormat()
    {
        // Arrange
        var data = new
        {
            company = "TechCorp",
            departments = new[]
            {
                new
                {
                    id = 1,
                    name = "Engineering",
                    employees = new[]
                    {
                        new { id = 101, name = "Alice" },
                        new { id = 102, name = "Bob" }
                    }
                }
            },
            metadata = new
            {
                created = "2024-01-01",
                tags = new[] { "tech", "startup" }
            }
        };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("company: TechCorp", result);
        Assert.Contains("departments[1]:", result);
        Assert.Contains("- id: 1", result);
        Assert.Contains("  employees[2]{id,name}:", result);
        Assert.Contains("metadata:", result);
        Assert.Contains("  tags[2]: tech,startup", result);
    }

    [Fact]
    public void Encode_NumberTypes_ReturnsCorrectFormats()
    {
        // Arrange
        var data = new
        {
            intValue = 42,
            longValue = 9223372036854775807L,
            doubleValue = 3.1415d,
            negativeValue = -15
        };

        var dv = $"doubleValue: {data.doubleValue}";

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("intValue: 42", result);
        Assert.Contains("longValue: 9223372036854775807", result);
        Assert.Contains("doubleValue: 3.1415", result);
        Assert.Contains("negativeValue: -15", result);
    }

    [Fact]
    public void Encode_SpecialStringCharacters_ReturnsEscapedStrings()
    {
        // Arrange
        var data = new
        {
            newline = "line1\nline2",
            tab = "col1\tcol2",
            quote = "say \"hello\"",
            backslash = "path\\to\\file"
        };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("\\n", result);
        Assert.Contains("\\t", result);
        Assert.Contains("\\\"", result);
        Assert.Contains("\\\\", result);
    }

    [Fact]
    public void Encode_RootArray_ReturnsArrayWithoutKey()
    {
        // Arrange
        var data = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.StartsWith("[5]:", result);
        Assert.Contains("1,2,3,4,5", result);
    }

    [Fact]
    public void Encode_RootTabularArray_ReturnsTabularWithoutKey()
    {
        // Arrange
        var data = new[]
        {
            new { id = 1, name = "Alice" },
            new { id = 2, name = "Bob" }
        };

        // Act
        var result = Toon.Encode(data);

        // Assert
        Assert.Contains("[2]{id,name}:", result);
        Assert.Contains("1,Alice", result);
    }

    [Fact]
    public void Encode_AllDelimiterOptions_UsesCorrectDelimiter()
    {
        // Arrange
        var data = new[] { "a", "b", "c" };

        // Act - Comma (default)
        var commaResult = Toon.Encode(data);
        Assert.Contains("a,b,c", commaResult);

        // Act - Pipe
        var pipeResult = Toon.Encode(data, new EncodeOptions { Delimiter = '|' });
        Assert.Contains("a|b|c", pipeResult);

        // Act - Tab
        var tabResult = Toon.Encode(data, new EncodeOptions { Delimiter = '\t' });
        Assert.Contains("a\tb\tc", tabResult);
    }

    [Fact]
    public void Encode_WithAllOptions_AppliesAllOptions()
    {
        // Arrange
        var data = new { items = new[] { 1, 2, 3 } };
        var options = new EncodeOptions
        {
            Indent = 4,
            Delimiter = '|',
            LengthMarker = '#'
        };

        // Act
        var result = Toon.Encode(data, options);

        // Assert
        Assert.Contains("[#3|]:", result); // Length marker and delimiter
        Assert.Contains("1|2|3", result); // Pipe delimiter
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public void Encode_VariousIndentSizes_UsesCorrectIndentation(int indentSize)
    {
        // Arrange
        var data = new { nested = new { value = 42 } };
        var options = new EncodeOptions { Indent = indentSize };

        // Act
        var result = Toon.Encode(data, options);

        // Assert
        var expectedIndent = new string(' ', indentSize);
        Assert.Contains($"{expectedIndent}value: 42", result);
    }

    [Fact]
    public void Encode_TabularArrayWithNullProperty_IncludesNull()
    {
        // Arrange - Uniform: both have all properties, first has explicit null
        var json = "[{\"id\":1,\"name\":\"Alice\",\"role\":null},{\"id\":2,\"name\":\"Bob\",\"role\":\"user\"}]";
        var data = new { users = JsonSerializer.Deserialize<JsonElement>(json) };

        // Act
        var result = Toon.Encode(data);

        // Assert - Should use tabular format with null
        Assert.Contains("users[2]{id,name,role}:", result);
        var lines = result.Split('\n');
        Assert.Contains("1,Alice,null", lines[1]); // First data row
        Assert.Contains("2,Bob,user", lines[2]); // Second data row
    }
}
