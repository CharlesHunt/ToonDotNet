namespace ToonFormat.Shared;

/// <summary>
/// Validation utilities for TOON format processing.
/// </summary>
internal static class ValidationUtils
{
    /// <summary>
    /// Validates that an array has the expected count.
    /// </summary>
    /// <param name="expected">The expected count.</param>
    /// <param name="actual">The actual count.</param>
    /// <param name="context">Context information for error messages.</param>
    /// <exception cref="InvalidOperationException">Thrown when counts don't match.</exception>
    public static void AssertExpectedCount(int expected, int actual, string context)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException($"Expected {expected} {context}, but found {actual}");
        }
    }

    /// <summary>
    /// Validates that there are no blank lines in a specified range.
    /// </summary>
    /// <param name="blankLines">List of blank line information.</param>
    /// <param name="startLine">Start line number (inclusive).</param>
    /// <param name="endLine">End line number (inclusive).</param>
    /// <exception cref="InvalidOperationException">Thrown when blank lines are found in the range.</exception>
    public static void ValidateNoBlankLinesInRange(List<BlankLineInfo> blankLines, int startLine, int endLine)
    {
        var blanksInRange = blankLines.Where(b => b.LineNumber >= startLine && b.LineNumber <= endLine).ToList();
        
        if (blanksInRange.Count > 0)
        {
            var lineNumbers = string.Join(", ", blanksInRange.Select(b => b.LineNumber));
            throw new InvalidOperationException($"Unexpected blank lines at lines: {lineNumbers}");
        }
    }

    /// <summary>
    /// Validates that there are no extra list items beyond what was declared.
    /// </summary>
    /// <param name="declaredCount">The declared number of items.</param>
    /// <param name="actualCount">The actual number of items found.</param>
    /// <exception cref="InvalidOperationException">Thrown when there are too many items.</exception>
    public static void ValidateNoExtraListItems(int declaredCount, int actualCount)
    {
        if (actualCount > declaredCount)
        {
            throw new InvalidOperationException($"Array declared {declaredCount} items but found {actualCount}");
        }
    }

    /// <summary>
    /// Validates that there are no extra tabular rows beyond what was declared.
    /// </summary>
    /// <param name="declaredCount">The declared number of rows.</param>
    /// <param name="actualCount">The actual number of rows found.</param>
    /// <exception cref="InvalidOperationException">Thrown when there are too many rows.</exception>
    public static void ValidateNoExtraTabularRows(int declaredCount, int actualCount)
    {
        if (actualCount > declaredCount)
        {
            throw new InvalidOperationException($"Tabular array declared {declaredCount} rows but found {actualCount}");
        }
    }
}