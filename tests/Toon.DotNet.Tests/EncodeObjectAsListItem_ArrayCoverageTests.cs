using System.Text.Json;

namespace ToonFormat.Tests;

/// <summary>
/// Additional tests to ensure 100% coverage of EncodeObjectAsListItem array handling.
/// </summary>
public class EncodeObjectAsListItem_ArrayCoverageTests
{
    [Fact]
    public void FirstPropertyArrayOfPrimitiveArrays_NestedInlineFormat()
    {
        // Covers: Array of arrays (not objects, not mixed)
        var json = "[{\"grid\":[[1,2,3],[4,5,6]],\"size\":2}]";
        var data = JsonSerializer.Deserialize<JsonElement>(json);
        var result = Toon.Encode(data);
        
        Assert.Contains("- grid[2]:", result);
        Assert.Contains("  - [3]: 1,2,3", result);
        Assert.Contains("  - [3]: 4,5,6", result);
        Assert.Contains("  size: 2", result);
    }

    [Fact]
    public void FirstPropertyEmptyArray_EmptyArrayHeader()
    {
        // Covers: Empty array branch
        var json = "[{\"items\":[],\"count\":0}]";
        var data = JsonSerializer.Deserialize<JsonElement>(json);
        var result = Toon.Encode(data);
        
        Assert.Contains("- items[0]:", result);
        Assert.Contains("  count: 0", result);
    }

    [Fact]
    public void FirstPropertySingleElementPrimitiveArray_InlineFormat()
    {
        // Covers: Single element array
        var data = new[] { new { item = new[] { 42 }, name = "single" } };
        var result = Toon.Encode(data);
        
        Assert.Contains("- item[1]: 42", result);
        Assert.Contains("  name: single", result);
    }

    [Fact]
    public void FirstPropertyArrayOfObjectsEmptyObjects_ListFormat()
    {
        // Covers: Array of empty objects
        var json = "[{\"items\":[{},{}],\"count\":2}]";
        var data = JsonSerializer.Deserialize<JsonElement>(json);
        var result = Toon.Encode(data);
        
        Assert.Contains("- items[2]:", result);
        Assert.Contains("  count: 2", result);
    }
}