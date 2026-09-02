using ToonFormat.Decode;

namespace ToonFormat.Tests;

/// <summary>
/// Comprehensive tests for ToonScanner to improve code coverage.
/// Tests line scanning, indentation detection, blank line handling, and validation.
/// </summary>
public class ToonScannerTests
{
    [Fact]
    public void ToParsedLines_EmptyString_ReturnsEmptyArrays()
    {
        // Arrange
        var input = "";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Empty(result.Lines);
        Assert.Empty(result.BlankLines);
    }

    [Fact]
    public void ToParsedLines_WhitespaceOnly_ReturnsEmptyArrays()
    {
        // Arrange
        var input = "   \n  \n    ";  // ← This contains NEWLINES!

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Empty(result.Lines);
        Assert.NotEmpty(result.BlankLines);  // ✅ Changed from Assert.Empty
        Assert.Equal(3, result.BlankLines.Length);
    }

    [Fact]
    public void ToParsedLines_SingleLine_ReturnsSingleParsedLine()
    {
        // Arrange
        var input = "name: Alice";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Single(result.Lines);
        Assert.Equal("name: Alice", result.Lines[0].Content);
        Assert.Equal(0, result.Lines[0].Depth);
        Assert.Equal(0, result.Lines[0].Indent);
        Assert.Equal(1, result.Lines[0].LineNumber);
    }

    [Fact]
    public void ToParsedLines_MultipleLines_ReturnsAllLines()
    {
        // Arrange
        var input = "name: Alice\nage: 30\nrole: admin";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal(3, result.Lines.Length);
        Assert.Equal("name: Alice", result.Lines[0].Content);
        Assert.Equal("age: 30", result.Lines[1].Content);
        Assert.Equal("role: admin", result.Lines[2].Content);
    }

    [Fact]
    public void ToParsedLines_IndentedLines_CalculatesCorrectDepth()
    {
        // Arrange
        var input = "level0:\n  level1:\n    level2:\n      level3:";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal(4, result.Lines.Length);
        Assert.Equal(0, result.Lines[0].Depth);
        Assert.Equal(1, result.Lines[1].Depth);
        Assert.Equal(2, result.Lines[2].Depth);
        Assert.Equal(3, result.Lines[3].Depth);
    }

    [Fact]
    public void ToParsedLines_CustomIndentSize_CalculatesCorrectDepth()
    {
        // Arrange
        var input = "level0:\n    level1:\n        level2:";

        // Act
        var result = ToonScanner.ToParsedLines(input, 4, false);

        // Assert
        Assert.Equal(3, result.Lines.Length);
        Assert.Equal(0, result.Lines[0].Depth);
        Assert.Equal(1, result.Lines[1].Depth);
        Assert.Equal(2, result.Lines[2].Depth);
    }

    [Fact]
    public void ToParsedLines_TracksIndentAmount()
    {
        // Arrange
        var input = "a:\n  b:\n    c:";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal(0, result.Lines[0].Indent);
        Assert.Equal(2, result.Lines[1].Indent);
        Assert.Equal(4, result.Lines[2].Indent);
    }

    [Fact]
    public void ToParsedLines_TracksLineNumbers()
    {
        // Arrange
        var input = "line1\nline2\nline3";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal(1, result.Lines[0].LineNumber);
        Assert.Equal(2, result.Lines[1].LineNumber);
        Assert.Equal(3, result.Lines[2].LineNumber);
    }

    [Fact]
    public void ToParsedLines_PreservesRawLine()
    {
        // Arrange
        var input = "  name: Alice";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal("  name: Alice", result.Lines[0].Raw);
        Assert.Equal("name: Alice", result.Lines[0].Content);
    }

    [Fact]
    public void ToParsedLines_BlankLineAtStart_TracksBlankLine()
    {
        // Arrange
        var input = "\nname: Alice";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Single(result.Lines);
        Assert.Single(result.BlankLines);
        Assert.Equal(1, result.BlankLines[0].LineNumber);
    }

    [Fact]
    public void ToParsedLines_BlankLineInMiddle_TracksBlankLine()
    {
        // Arrange
        var input = "name: Alice\n\nage: 30";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal(2, result.Lines.Length);
        Assert.Single(result.BlankLines);
        Assert.Equal(2, result.BlankLines[0].LineNumber);
    }

    [Fact]
    public void ToParsedLines_BlankLineAtEnd_TracksBlankLine()
    {
        // Arrange
        var input = "name: Alice\n";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Single(result.Lines);
        Assert.Single(result.BlankLines);
        Assert.Equal(2, result.BlankLines[0].LineNumber);
    }

    [Fact]
    public void ToParsedLines_MultipleBlankLines_TracksAllBlankLines()
    {
        // Arrange
        var input = "a\n\n\nb";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal(2, result.Lines.Length);
        Assert.Equal(2, result.BlankLines.Length);
        Assert.Equal(2, result.BlankLines[0].LineNumber);
        Assert.Equal(3, result.BlankLines[1].LineNumber);
    }

    [Fact]
    public void ToParsedLines_IndentedBlankLine_TracksIndentAndDepth()
    {
        // Arrange
        var input = "a:\n  \n  b:";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal(2, result.Lines.Length);
        Assert.Single(result.BlankLines);
        Assert.Equal(2, result.BlankLines[0].Indent);
        Assert.Equal(1, result.BlankLines[0].Depth);
    }

    [Fact]
    public void ToParsedLines_StrictMode_InvalidIndentation_ThrowsException()
    {
        // Arrange
        var input = "a:\n   b:"; // 3 spaces instead of 2

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => ToonScanner.ToParsedLines(input, 2, true));
        Assert.Contains("Invalid indentation", exception.Message);
        Assert.Contains("Line 2", exception.Message);
    }

    [Fact]
    public void ToParsedLines_StrictMode_TabInIndentation_ThrowsException()
    {
        // Arrange
        var input = "a:\n\tb:"; // Tab instead of spaces

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => ToonScanner.ToParsedLines(input, 2, true));
        Assert.Contains("Tabs are not allowed", exception.Message);
        Assert.Contains("Line 2", exception.Message);
    }

    [Fact]
    public void ToParsedLines_StrictMode_ValidIndentation_Succeeds()
    {
        // Arrange
        var input = "a:\n  b:\n    c:";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, true);

        // Assert
        Assert.Equal(3, result.Lines.Length);
    }

    [Fact]
    public void ToParsedLines_NonStrictMode_InvalidIndentation_Succeeds()
    {
        // Arrange
        var input = "a:\n   b:"; // 3 spaces instead of 2

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal(2, result.Lines.Length);
        Assert.Equal(1, result.Lines[1].Depth); // Depth calculated as 3/2=1
    }

    [Fact]
    public void ToParsedLines_NonStrictMode_TabInIndentation_Succeeds()
    {
        // Arrange
        var input = "a:\n\tb:";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal(2, result.Lines.Length);
    }

    [Fact]
    public void ToParsedLines_DepthCalculation_ZeroIndent_ReturnsZeroDepth()
    {
        // Arrange
        var input = "value";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal(0, result.Lines[0].Depth);
    }

    [Fact]
    public void ToParsedLines_DepthCalculation_ExactMultiple_ReturnsCorrectDepth()
    {
        // Arrange
        var input = "      value"; // 6 spaces with indent size 2

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal(3, result.Lines[0].Depth);
        Assert.Equal(6, result.Lines[0].Indent);
    }

    [Fact]
    public void ToParsedLines_DepthCalculation_NonExactMultiple_TruncatesDepth()
    {
        // Arrange
        var input = "     value"; // 5 spaces with indent size 2

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal(2, result.Lines[0].Depth); // 5/2=2 (integer division)
        Assert.Equal(5, result.Lines[0].Indent);
    }

    [Fact]
    public void ToParsedLines_VariousIndentSizes_CalculatesCorrectDepth()
    {
        // Arrange
        var input = "        value"; // 8 spaces

        // Act - Indent size 2
        var result2 = ToonScanner.ToParsedLines(input, 2, false);
        Assert.Equal(4, result2.Lines[0].Depth);

        // Act - Indent size 4
        var result4 = ToonScanner.ToParsedLines(input, 4, false);
        Assert.Equal(2, result4.Lines[0].Depth);

        // Act - Indent size 8
        var result8 = ToonScanner.ToParsedLines(input, 8, false);
        Assert.Equal(1, result8.Lines[0].Depth);
    }

    [Fact]
    public void ToParsedLines_MixedContent_ParsesCorrectly()
    {
        // Arrange
        var input = @"name: Alice
age: 30
address:
  street: 123 Main St
  city: Boston

hobbies[3]:
  - reading
  - coding
  - gaming";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal(9, result.Lines.Length);
        Assert.Single(result.BlankLines);
        
        // Check depths
        Assert.Equal(0, result.Lines[0].Depth); // name
        Assert.Equal(0, result.Lines[1].Depth); // age
        Assert.Equal(0, result.Lines[2].Depth); // address
        Assert.Equal(1, result.Lines[3].Depth); // street
        Assert.Equal(1, result.Lines[4].Depth); // city
        Assert.Equal(0, result.Lines[5].Depth); // hobbies
        Assert.Equal(1, result.Lines[6].Depth); // - reading
        Assert.Equal(1, result.Lines[7].Depth); // - coding
        Assert.Equal(1, result.Lines[8].Depth); // - gaming
    }

    [Fact]
    public void ToParsedLines_TrailingSpaces_PreservedInRaw()
    {
        // Arrange
        var input = "value   ";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal("value   ", result.Lines[0].Raw);
        Assert.Equal("value   ", result.Lines[0].Content);
    }

    [Fact]
    public void ToParsedLines_LeadingSpacesOnly_RemovesLeadingSpaces()
    {
        // Arrange
        var input = "    value";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal("    value", result.Lines[0].Raw);
        Assert.Equal("value", result.Lines[0].Content);
        Assert.Equal(4, result.Lines[0].Indent);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData(" \t ")]
    public void ToParsedLines_WhitespaceLine_TreatedAsBlank(string whitespace)
    {
        // Arrange
        var input = $"a\n{whitespace}\nb";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal(2, result.Lines.Length);
        Assert.Single(result.BlankLines);
    }

    [Fact]
    public void ToParsedLines_ComplexRealWorldExample_ParsesCorrectly()
    {
        // Arrange
        var input = @"company: TechCorp
departments[2]{id,name}:
  1,Engineering
  2,Sales
metadata:
  created: 2024-01-01
  tags[3]: prod,stable,v1";

        // Act
        var result = ToonScanner.ToParsedLines(input, 2, false);

        // Assert
        Assert.Equal(7, result.Lines.Length);
        Assert.Empty(result.BlankLines);
        
        // Verify content
        Assert.Contains("TechCorp", result.Lines[0].Content);
        Assert.Contains("departments", result.Lines[1].Content);
        Assert.Contains("Engineering", result.Lines[2].Content);
        Assert.Contains("metadata", result.Lines[4].Content);
        
        // Verify depths
        Assert.Equal(0, result.Lines[0].Depth);
        Assert.Equal(0, result.Lines[1].Depth);
        Assert.Equal(1, result.Lines[2].Depth);
        Assert.Equal(1, result.Lines[3].Depth);
        Assert.Equal(0, result.Lines[4].Depth);
        Assert.Equal(1, result.Lines[5].Depth);
        Assert.Equal(1, result.Lines[6].Depth);
    }

    [Fact]
    public void LineCursor_Initialization_StartsAtBeginning()
    {
        // Arrange
        var lines = new[] { CreateParsedLine("a", 0, 0, 0, 1) };
        var blankLines = Array.Empty<BlankLineInfo>();

        // Act
        var cursor = new LineCursor(lines, blankLines);

        // Assert
        Assert.False(cursor.AtEnd);
        Assert.Equal(1, cursor.Length);
    }

    [Fact]
    public void LineCursor_Peek_ReturnsSameLineMultipleTimes()
    {
        // Arrange
        var lines = new[] { CreateParsedLine("a", 0, 0, 0, 1) };
        var cursor = new LineCursor(lines, Array.Empty<BlankLineInfo>());

        // Act
        var first = cursor.Peek();
        var second = cursor.Peek();

        // Assert
        Assert.Same(first, second);
        Assert.Equal("a", first?.Content);
    }

    [Fact]
    public void LineCursor_Next_AdvancesCursor()
    {
        // Arrange
        var lines = new[]
        {
            CreateParsedLine("a", 0, 0, 0, 1),
            CreateParsedLine("b", 0, 0, 0, 2)
        };
        var cursor = new LineCursor(lines, Array.Empty<BlankLineInfo>());

        // Act
        var first = cursor.Next();
        var second = cursor.Next();

        // Assert
        Assert.Equal("a", first?.Content);
        Assert.Equal("b", second?.Content);
    }

    [Fact]
    public void LineCursor_Advance_MovesForwardWithoutReturning()
    {
        // Arrange
        var lines = new[]
        {
            CreateParsedLine("a", 0, 0, 0, 1),
            CreateParsedLine("b", 0, 0, 0, 2)
        };
        var cursor = new LineCursor(lines, Array.Empty<BlankLineInfo>());

        // Act
        cursor.Advance();
        var current = cursor.Peek();

        // Assert
        Assert.Equal("b", current?.Content);
    }

    [Fact]
    public void LineCursor_Current_ReturnsPreviousLine()
    {
        // Arrange
        var lines = new[]
        {
            CreateParsedLine("a", 0, 0, 0, 1),
            CreateParsedLine("b", 0, 0, 0, 2)
        };
        var cursor = new LineCursor(lines, Array.Empty<BlankLineInfo>());

        // Act
        cursor.Advance();
        var current = cursor.Current();

        // Assert
        Assert.Equal("a", current?.Content);
    }

    [Fact]
    public void LineCursor_Current_ReturnsNullAtStart()
    {
        // Arrange
        var lines = new[] { CreateParsedLine("a", 0, 0, 0, 1) };
        var cursor = new LineCursor(lines, Array.Empty<BlankLineInfo>());

        // Act
        var current = cursor.Current();

        // Assert
        Assert.Null(current);
    }

    [Fact]
    public void LineCursor_AtEnd_TrueWhenExhausted()
    {
        // Arrange
        var lines = new[] { CreateParsedLine("a", 0, 0, 0, 1) };
        var cursor = new LineCursor(lines, Array.Empty<BlankLineInfo>());

        // Act
        cursor.Advance();

        // Assert
        Assert.True(cursor.AtEnd);
    }

    [Fact]
    public void LineCursor_PeekAtDepth_ReturnsLineAtTargetDepth()
    {
        // Arrange
        var lines = new[]
        {
            CreateParsedLine("a", 0, 0, 0, 1),
            CreateParsedLine("b", 1, 2, 2, 2),
            CreateParsedLine("c", 2, 4, 4, 3)
        };
        var cursor = new LineCursor(lines, Array.Empty<BlankLineInfo>());

        // Act
        cursor.Advance(); // Move to line with depth 1
        var line = cursor.PeekAtDepth(1);

        // Assert
        Assert.NotNull(line);
        Assert.Equal("b", line.Content);
    }

    [Fact]
    public void LineCursor_PeekAtDepth_ReturnsNullForDeeperDepth()
    {
        // Arrange
        var lines = new[] { CreateParsedLine("a", 0, 0, 0, 1) };
        var cursor = new LineCursor(lines, Array.Empty<BlankLineInfo>());

        // Act
        var line = cursor.PeekAtDepth(1);

        // Assert
        Assert.Null(line);
    }

    [Fact]
    public void LineCursor_PeekAtDepth_ReturnsNullForShallowerDepth()
    {
        // Arrange
        var lines = new[] { CreateParsedLine("a", 2, 4, 4, 1) };
        var cursor = new LineCursor(lines, Array.Empty<BlankLineInfo>());

        // Act
        var line = cursor.PeekAtDepth(1);

        // Assert
        Assert.Null(line);
    }

    [Fact]
    public void LineCursor_HasMoreAtDepth_TrueWhenLineExistsAtDepth()
    {
        // Arrange
        var lines = new[] { CreateParsedLine("a", 1, 2, 2, 1) };
        var cursor = new LineCursor(lines, Array.Empty<BlankLineInfo>());

        // Act
        var hasMore = cursor.HasMoreAtDepth(1);

        // Assert
        Assert.True(hasMore);
    }

    [Fact]
    public void LineCursor_HasMoreAtDepth_FalseWhenNoLineAtDepth()
    {
        // Arrange
        var lines = new[] { CreateParsedLine("a", 0, 0, 0, 1) };
        var cursor = new LineCursor(lines, Array.Empty<BlankLineInfo>());

        // Act
        var hasMore = cursor.HasMoreAtDepth(1);

        // Assert
        Assert.False(hasMore);
    }

    [Fact]
    public void LineCursor_BlankLines_ReturnsBlankLineInfo()
    {
        // Arrange
        var blankLines = new[]
        {
            new BlankLineInfo { LineNumber = 2, Indent = 0, Depth = 0 }
        };
        var cursor = new LineCursor(Array.Empty<ParsedLine>(), blankLines);

        // Act
        var result = cursor.BlankLines;

        // Assert
        Assert.Same(blankLines, result);
    }

    // Helper method to create ParsedLine instances
    private static ParsedLine CreateParsedLine(string content, int depth, int indent, int indentSize, int lineNumber)
    {
        return new ParsedLine
        {
            Raw = new string(' ', indent) + content,
            Content = content,
            Depth = depth,
            Indent = indent,
            LineNumber = lineNumber
        };
    }
}
