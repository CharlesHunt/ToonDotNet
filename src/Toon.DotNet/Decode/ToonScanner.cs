namespace ToonFormat.Decode;

/// <summary>
/// Result of scanning TOON input lines.
/// </summary>
internal class ScanResult
{
    /// <summary>
    /// Parsed lines with content.
    /// </summary>
#if NETSTANDARD2_0
    public ParsedLine[] Lines { get; set; }
#else
    public required ParsedLine[] Lines { get; set; }
#endif

    /// <summary>
    /// Information about blank lines.
    /// </summary>
#if NETSTANDARD2_0
    public BlankLineInfo[] BlankLines { get; set; }
#else
    public required BlankLineInfo[] BlankLines { get; set; }
#endif
}

/// <summary>
/// Cursor for iterating through parsed lines during decoding.
/// </summary>
internal class LineCursor
{
    private readonly ParsedLine[] _lines;
    private int _index;
    private readonly BlankLineInfo[] _blankLines;

    /// <summary>
    /// Initializes a new LineCursor.
    /// </summary>
    /// <param name="lines">The parsed lines to iterate through.</param>
    /// <param name="blankLines">Information about blank lines.</param>
    public LineCursor(ParsedLine[] lines, BlankLineInfo[] blankLines)
    {
        _lines = lines;
        _index = 0;
        _blankLines = blankLines;
    }

    /// <summary>
    /// Gets information about blank lines.
    /// </summary>
    public BlankLineInfo[] BlankLines => _blankLines;

    /// <summary>
    /// Peeks at the current line without advancing the cursor.
    /// </summary>
    public ParsedLine? Peek()
    {
        return _index < _lines.Length ? _lines[_index] : null;
    }

    /// <summary>
    /// Gets the current line and advances the cursor.
    /// </summary>
    public ParsedLine? Next()
    {
        return _index < _lines.Length ? _lines[_index++] : null;
    }

    /// <summary>
    /// Gets the previously read line.
    /// </summary>
    public ParsedLine? Current()
    {
        return _index > 0 ? _lines[_index - 1] : null;
    }

    /// <summary>
    /// Advances the cursor without returning a line.
    /// </summary>
    public void Advance()
    {
        _index++;
    }

    /// <summary>
    /// Checks if the cursor is at the end of the lines.
    /// </summary>
    public bool AtEnd => _index >= _lines.Length;

    /// <summary>
    /// Gets the total number of lines.
    /// </summary>
    public int Length => _lines.Length;

    /// <summary>
    /// Peeks at the current line if it's at the target depth.
    /// </summary>
    /// <param name="targetDepth">The depth to check for.</param>
    /// <returns>The line if it's at the target depth, null otherwise.</returns>
    public ParsedLine? PeekAtDepth(int targetDepth)
    {
        var line = Peek();
        if (line == null || line.Depth < targetDepth)
            return null;
        
        if (line.Depth == targetDepth)
            return line;
        
        return null;
    }

    /// <summary>
    /// Checks if there are more lines at the target depth.
    /// </summary>
    /// <param name="targetDepth">The depth to check for.</param>
    /// <returns>True if there are more lines at the target depth.</returns>
    public bool HasMoreAtDepth(int targetDepth)
    {
        return PeekAtDepth(targetDepth) != null;
    }
}

/// <summary>
/// Scanner for parsing TOON input into structured lines.
/// </summary>
internal static class ToonScanner
{
    /// <summary>
    /// Parses TOON input text into structured lines.
    /// </summary>
    /// <param name="source">The TOON input text.</param>
    /// <param name="indentSize">The number of spaces per indentation level.</param>
    /// <param name="strict">Whether to enforce strict validation.</param>
    /// <returns>Scan result with parsed lines and blank line information.</returns>
    public static ScanResult ToParsedLines(string source, int indentSize, bool strict)
    {
        if (string.IsNullOrWhiteSpace(source) && !source.Contains("\n"))
        {
            return new ScanResult 
            { 
                Lines = Array.Empty<ParsedLine>(), 
                BlankLines = Array.Empty<BlankLineInfo>() 
            };
        }

        var lines = source.Split('\n');
        var parsed = new List<ParsedLine>();
        var blankLines = new List<BlankLineInfo>();

        for (int i = 0; i < lines.Length; i++)
        {
            string raw = lines[i];
            int lineNumber = i + 1;
            int indent = 0;
            
            while (indent < raw.Length && raw[indent] == Constants.Space)
            {
                indent++;
            }

#if NETSTANDARD2_0
            string content = raw.Substring(indent);
#else
            string content = raw[indent..];
#endif

            // Track blank lines
            if (string.IsNullOrWhiteSpace(content))
            {
                int depth = ComputeDepthFromIndent(indent, indentSize);
                blankLines.Add(new BlankLineInfo
                {
                    LineNumber = lineNumber,
                    Indent = indent,
                    Depth = depth
                });
                continue;
            }

            int lineDepth = ComputeDepthFromIndent(indent, indentSize);

            // Strict mode validation
            if (strict)
            {
                // Find the full leading whitespace region (spaces and tabs)
                int wsEnd = 0;
                while (wsEnd < raw.Length && (raw[wsEnd] == Constants.Space || raw[wsEnd] == Constants.Tab))
                {
                    wsEnd++;
                }

                // Check for tabs in leading whitespace (before actual content)
                if (
#if NETSTANDARD2_0
                    raw.Substring(0, wsEnd).Contains(Constants.Tab)
#else
                    raw[..wsEnd].Contains(Constants.Tab)
#endif
                )
                {
                    throw new InvalidOperationException($"Line {lineNumber}: Tabs are not allowed in indentation. Use spaces only.");
                }

                // Check for mixed indentation patterns
                if (indent % indentSize != 0)
                {
                    throw new InvalidOperationException($"Line {lineNumber}: Invalid indentation. Expected multiple of {indentSize} spaces, got {indent}.");
                }
            }

            parsed.Add(new ParsedLine
            {
                Raw = raw,
                Depth = lineDepth,
                Indent = indent,
                Content = content,
                LineNumber = lineNumber
            });
        }

        return new ScanResult
        {
            Lines = parsed.ToArray(),
            BlankLines = blankLines.ToArray()
        };
    }

    /// <summary>
    /// Computes the depth level from the indent amount.
    /// </summary>
    /// <param name="indent">The number of spaces of indentation.</param>
    /// <param name="indentSize">The number of spaces per indentation level.</param>
    /// <returns>The computed depth level.</returns>
    private static int ComputeDepthFromIndent(int indent, int indentSize)
    {
        return indent / indentSize;
    }
}