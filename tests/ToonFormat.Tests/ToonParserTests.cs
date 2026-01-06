using System.Text.Json;
using ToonFormat.Decode;

namespace ToonFormat.Tests;

/// <summary>
/// Comprehensive tests for ToonParser internal methods to improve code coverage.
/// Tests focus on edge cases and error handling for parser utilities.
/// </summary>
public class ToonParserTests
{
    [Fact]
    public void ParseDelimitedValues_SimpleValues_ReturnsSplitArray()
    {
        // Arrange
        var input = "apple,banana,cherry";

        // Act
        var result = ToonParser.ParseDelimitedValues(input, ',');

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("apple", result[0]);
        Assert.Equal("banana", result[1]);
        Assert.Equal("cherry", result[2]);
    }

    [Fact]
    public void ParseDelimitedValues_WithQuotedValues_RespectsQuotes()
    {
        // Arrange
        var input = "\"hello, world\",simple,\"another, value\"";

        // Act
        var result = ToonParser.ParseDelimitedValues(input, ',');

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("\"hello, world\"", result[0]);
        Assert.Equal("simple", result[1]);
        Assert.Equal("\"another, value\"", result[2]);
    }

    [Fact]
    public void ParseDelimitedValues_WithEscapedQuotes_HandlesEscapes()
    {
        // Arrange
        var input = "\"say \\\"hello\\\"\",normal";

        // Act
        var result = ToonParser.ParseDelimitedValues(input, ',');

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("\"say \\\"hello\\\"\"", result[0]);
        Assert.Equal("normal", result[1]);
    }

    [Fact]
    public void ParseDelimitedValues_PipeDelimiter_SplitsCorrectly()
    {
        // Arrange
        var input = "first|second|third";

        // Act
        var result = ToonParser.ParseDelimitedValues(input, '|');

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("first", result[0]);
        Assert.Equal("second", result[1]);
        Assert.Equal("third", result[2]);
    }

    [Fact]
    public void ParseDelimitedValues_TabDelimiter_SplitsCorrectly()
    {
        // Arrange
        var input = "first\tsecond\tthird";

        // Act
        var result = ToonParser.ParseDelimitedValues(input, '\t');

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("first", result[0]);
        Assert.Equal("second", result[1]);
        Assert.Equal("third", result[2]);
    }

    [Fact]
    public void ParseDelimitedValues_EmptyString_ReturnsEmptyArray()
    {
        // Arrange
        var input = "";

        // Act
        var result = ToonParser.ParseDelimitedValues(input, ',');

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseDelimitedValues_SingleValue_ReturnsSingleElementArray()
    {
        // Arrange
        var input = "single";

        // Act
        var result = ToonParser.ParseDelimitedValues(input, ',');

        // Assert
        Assert.Single(result);
        Assert.Equal("single", result[0]);
    }

    [Fact]
    public void ParseDelimitedValues_TrailingDelimiter_IncludesEmptyValue()
    {
        // Arrange
        var input = "first,second,";

        // Act
        var result = ToonParser.ParseDelimitedValues(input, ',');

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("first", result[0]);
        Assert.Equal("second", result[1]);
        Assert.Equal("", result[2]);
    }

    [Fact]
    public void ParseDelimitedValues_LeadingDelimiter_IncludesEmptyValue()
    {
        // Arrange
        var input = ",second,third";

        // Act
        var result = ToonParser.ParseDelimitedValues(input, ',');

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("", result[0]);
        Assert.Equal("second", result[1]);
        Assert.Equal("third", result[2]);
    }

    [Fact]
    public void ParseDelimitedValues_ConsecutiveDelimiters_IncludesEmptyValues()
    {
        // Arrange
        var input = "first,,third";

        // Act
        var result = ToonParser.ParseDelimitedValues(input, ',');

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("first", result[0]);
        Assert.Equal("", result[1]);
        Assert.Equal("third", result[2]);
    }

    [Fact]
    public void ParseDelimitedValues_WithWhitespace_TrimsValues()
    {
        // Arrange
        var input = " first , second , third ";

        // Act
        var result = ToonParser.ParseDelimitedValues(input, ',');

        // Assert
        Assert.Equal("first", result[0]);
        Assert.Equal("second", result[1]);
        Assert.Equal("third", result[2]);
    }

    [Fact]
    public void MapRowValuesToPrimitives_Numbers_ReturnsNumberElements()
    {
        // Arrange
        var values = new[] { "1", "2", "3" };

        // Act
        var result = ToonParser.MapRowValuesToPrimitives(values);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal(1, result[0].GetInt32());
        Assert.Equal(2, result[1].GetInt32());
        Assert.Equal(3, result[2].GetInt32());
    }

    [Fact]
    public void MapRowValuesToPrimitives_MixedTypes_ReturnsCorrectElements()
    {
        // Arrange
        var values = new[] { "42", "\"text\"", "true", "null" };

        // Act
        var result = ToonParser.MapRowValuesToPrimitives(values);

        // Assert
        Assert.Equal(4, result.Length);
        Assert.Equal(42, result[0].GetInt32());
        Assert.Equal("text", result[1].GetString());
        Assert.True(result[2].GetBoolean());
        Assert.Equal(JsonValueKind.Null, result[3].ValueKind);
    }

    [Fact]
    public void MapRowValuesToPrimitives_EmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        var values = Array.Empty<string>();

        // Act
        var result = ToonParser.MapRowValuesToPrimitives(values);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseStringLiteral_UnquotedString_ReturnsTrimmedValue()
    {
        // Arrange
        var input = "  hello  ";

        // Act
        var result = ToonParser.ParseStringLiteral(input);

        // Assert
        Assert.Equal("hello", result);
    }

    [Fact]
    public void ParseStringLiteral_QuotedString_ReturnsUnquotedValue()
    {
        // Arrange
        var input = "\"hello world\"";

        // Act
        var result = ToonParser.ParseStringLiteral(input);

        // Assert
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void ParseStringLiteral_QuotedStringWithEscapes_ReturnsUnescapedValue()
    {
        // Arrange
        var input = "\"hello\\nworld\"";

        // Act
        var result = ToonParser.ParseStringLiteral(input);

        // Assert
        Assert.Equal("hello\nworld", result);
    }

    [Fact]
    public void ParseStringLiteral_QuotedStringWithEscapedQuotes_ReturnsCorrectValue()
    {
        // Arrange
        var input = "\"say \\\"hello\\\"\"";

        // Act
        var result = ToonParser.ParseStringLiteral(input);

        // Assert
        Assert.Equal("say \"hello\"", result);
    }

    [Fact]
    public void ParseStringLiteral_UnterminatedQuote_ThrowsException()
    {
        // Arrange
        var input = "\"unterminated";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ToonParser.ParseStringLiteral(input));
        Assert.Contains("Unterminated string", exception.Message);
    }

    [Fact]
    public void ParseStringLiteral_CharactersAfterClosingQuote_ThrowsException()
    {
        // Arrange
        var input = "\"hello\" extra";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ToonParser.ParseStringLiteral(input));
        Assert.Contains("Unexpected characters after closing quote", exception.Message);
    }

    [Fact]
    public void ParseStringLiteral_EmptyQuotedString_ReturnsEmpty()
    {
        // Arrange
        var input = "\"\"";

        // Act
        var result = ToonParser.ParseStringLiteral(input);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void IsArrayHeaderAfterHyphen_ValidArrayHeader_ReturnsTrue()
    {
        // Arrange
        var input = "[3]: 1,2,3";

        // Act
        var result = ToonParser.IsArrayHeaderAfterHyphen(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsArrayHeaderAfterHyphen_NoColon_ReturnsFalse()
    {
        // Arrange
        var input = "[3] some text";

        // Act
        var result = ToonParser.IsArrayHeaderAfterHyphen(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsArrayHeaderAfterHyphen_NoBracket_ReturnsFalse()
    {
        // Arrange
        var input = "some text: value";

        // Act
        var result = ToonParser.IsArrayHeaderAfterHyphen(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsArrayHeaderAfterHyphen_WithWhitespace_ReturnsTrue()
    {
        // Arrange
        var input = "  [3]:  1,2,3";

        // Act
        var result = ToonParser.IsArrayHeaderAfterHyphen(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsObjectFirstFieldAfterHyphen_ValidKeyValue_ReturnsTrue()
    {
        // Arrange
        var input = "name: Alice";

        // Act
        var result = ToonParser.IsObjectFirstFieldAfterHyphen(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsObjectFirstFieldAfterHyphen_NoColon_ReturnsFalse()
    {
        // Arrange
        var input = "just text";

        // Act
        var result = ToonParser.IsObjectFirstFieldAfterHyphen(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsObjectFirstFieldAfterHyphen_QuotedKeyWithColon_ReturnsTrue()
    {
        // Arrange
        var input = "\"key with: colon\": value";

        // Act
        var result = ToonParser.IsObjectFirstFieldAfterHyphen(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ParseArrayHeaderLine_SimpleArray_ReturnsCorrectHeader()
    {
        // Arrange
        var input = "items[3]: 1,2,3";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.NotNull(result);
        Assert.Equal("items", result.Header.Key);
        Assert.Equal(3, result.Header.Length);
        Assert.Equal(',', result.Header.Delimiter);
        Assert.NotNull(result.InlineValues);
        Assert.Equal(3, result.InlineValues.Length);
    }

    [Fact]
    public void ParseArrayHeaderLine_TabularArray_ReturnsCorrectHeader()
    {
        // Arrange
        var input = "users[2]{id,name}:";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.NotNull(result);
        Assert.Equal("users", result.Header.Key);
        Assert.Equal(2, result.Header.Length);
        Assert.NotNull(result.Header.Fields);
        Assert.Equal(2, result.Header.Fields.Length);
        Assert.Equal("id", result.Header.Fields[0]);
        Assert.Equal("name", result.Header.Fields[1]);
    }

    [Fact]
    public void ParseArrayHeaderLine_WithPipeDelimiter_ReturnsCorrectDelimiter()
    {
        // Arrange
        var input = "items[3|]: 1|2|3";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.NotNull(result);
        Assert.Equal('|', result.Header.Delimiter);
    }

    [Fact]
    public void ParseArrayHeaderLine_WithTabDelimiter_ReturnsCorrectDelimiter()
    {
        // Arrange
        var input = "items[3\t]: 1\t2\t3";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.NotNull(result);
        Assert.Equal('\t', result.Header.Delimiter);
    }

    [Fact]
    public void ParseArrayHeaderLine_WithLengthMarker_SetsMarkerFlag()
    {
        // Arrange
        var input = "items[#3]: 1,2,3";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Header.HasLengthMarker);
        Assert.Equal(3, result.Header.Length);
    }

    [Fact]
    public void ParseArrayHeaderLine_QuotedKey_ParsesKeyCorrectly()
    {
        // Arrange
        var input = "\"my items\"[2]: 1,2";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.NotNull(result);
        Assert.Equal("my items", result.Header.Key);
    }

    [Fact]
    public void ParseArrayHeaderLine_QuotedKeyWithBrackets_ParsesCorrectly()
    {
        // Arrange
        var input = "\"key[with]brackets\"[2]: 1,2";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.NotNull(result);
        Assert.Equal("key[with]brackets", result.Header.Key);
        Assert.Equal(2, result.Header.Length);
    }

    [Fact]
    public void ParseArrayHeaderLine_RootArray_NoKey()
    {
        // Arrange
        var input = "[3]: 1,2,3";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Header.Key);
        Assert.Equal(3, result.Header.Length);
    }

    [Fact]
    public void ParseArrayHeaderLine_EmptyArray_ReturnsZeroLength()
    {
        // Arrange
        var input = "items[0]:";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Header.Length);
    }

    [Fact]
    public void ParseArrayHeaderLine_NoColon_ReturnsNull()
    {
        // Arrange
        var input = "items[3]";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseArrayHeaderLine_NoBrackets_ReturnsNull()
    {
        // Arrange
        var input = "items: value";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseArrayHeaderLine_InvalidLength_ReturnsNull()
    {
        // Arrange
        var input = "items[abc]: value";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseArrayHeaderLine_UnterminatedBracket_ReturnsNull()
    {
        // Arrange
        var input = "items[3: value";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseArrayHeaderLine_WithFieldsAndInlineValues_ParsesBoth()
    {
        // Arrange
        var input = "data[2]{id,name}: 1,Alice";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Header.Fields!.Length);
        Assert.NotNull(result.InlineValues);
        Assert.Equal(2, result.InlineValues.Length);
    }

    [Fact]
    public void ParseArrayHeaderLine_QuotedFields_ParsesCorrectly()
    {
        // Arrange
        var input = "data[2]{\"first name\",\"last name\"}:";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.NotNull(result);
        Assert.Equal("first name", result.Header.Fields![0]);
        Assert.Equal("last name", result.Header.Fields[1]);
    }

    [Fact]
    public void ParseArrayHeaderLine_NoInlineValues_ReturnsNullInlineValues()
    {
        // Arrange
        var input = "items[3]:";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.InlineValues);
    }

    [Fact]
    public void ParseArrayHeaderLine_WithWhitespace_ParsesCorrectly()
    {
        // Arrange
        var input = "  items  [  3  ]  :  1,2,3  ";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.NotNull(result);
        Assert.Equal("items", result.Header.Key);
        Assert.Equal(3, result.Header.Length);
    }

    [Fact]
    public void ParseArrayHeaderLine_ComplexTabularWithPipe_ParsesCorrectly()
    {
        // Arrange
        var input = "users[2|]{id,name,role}:";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.NotNull(result);
        Assert.Equal("users", result.Header.Key);
        Assert.Equal(2, result.Header.Length);
        Assert.Equal('|', result.Header.Delimiter);
        Assert.Equal(3, result.Header.Fields!.Length);
    }

    [Fact]
    public void ParseArrayHeaderLine_LengthMarkerWithPipe_ParsesCorrectly()
    {
        // Arrange
        var input = "items[#3|]: 1|2|3";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Header.HasLengthMarker);
        Assert.Equal(3, result.Header.Length);
        Assert.Equal('|', result.Header.Delimiter);
    }

    [Fact]
    public void ParseArrayHeaderLine_ZeroLengthWithFields_ParsesCorrectly()
    {
        // Arrange
        var input = "items[0]{id,name}:";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Header.Length);
        Assert.NotNull(result.Header.Fields);
        Assert.Equal(2, result.Header.Fields.Length);
    }

    [Theory]
    [InlineData("items[5]: 1,2,3,4,5", 5)]
    [InlineData("items[10]: " + "1,2,3,4,5,6,7,8,9,10", 10)]
    [InlineData("items[1]: 42", 1)]
    public void ParseArrayHeaderLine_VariousLengths_ParsesCorrectly(string input, int expectedLength)
    {
        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedLength, result.Header.Length);
    }

    [Fact]
    public void ParseArrayHeaderLine_UnterminatedQuotedKey_ReturnsNull()
    {
        // Arrange
        var input = "\"unterminated[3]: value";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseArrayHeaderLine_QuotedKeyWithoutBracketAfter_ReturnsNull()
    {
        // Arrange
        var input = "\"key\": value";

        // Act
        var result = ToonParser.ParseArrayHeaderLine(input, ',');

        // Assert
        Assert.Null(result);
    }
}
