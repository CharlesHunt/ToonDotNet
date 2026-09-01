using System.Text;

namespace ToonFormat.Encode;

/// <summary>
/// Utility class for writing indented lines during TOON encoding.
/// </summary>
internal class LineWriter
{
    private readonly List<string> _lines;
    private readonly int _indentSize;

    /// <summary>
    /// Initializes a new instance of the LineWriter class.
    /// </summary>
    /// <param name="indentSize">The number of spaces per indentation level.</param>
    public LineWriter(int indentSize)
    {
        _lines = new List<string>();
        _indentSize = indentSize;
    }

    /// <summary>
    /// Adds a line with the specified indentation depth.
    /// </summary>
    /// <param name="depth">The indentation depth.</param>
    /// <param name="content">The line content.</param>
    public void Push(int depth, string content)
    {
        string indent = new string(Constants.Space, depth * _indentSize);
        _lines.Add(indent + content);
    }

    /// <summary>
    /// Returns the accumulated lines as a single string.
    /// </summary>
    /// <returns>All lines joined with newline characters.</returns>
    public override string ToString()
    {
        return string.Join("\n", _lines);
    }

    /// <summary>
    /// Gets the number of lines written.
    /// </summary>
    public int Count => _lines.Count;

    /// <summary>
    /// Checks if any lines have been written.
    /// </summary>
    public bool IsEmpty => _lines.Count == 0;
}