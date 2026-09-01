using System.Text.Json;

namespace ToonFormat.Encode;

/// <summary>
/// Utilities for normalizing input values to JsonElement for encoding.
/// </summary>
internal static class Normalizer
{
    /// <summary>
    /// Normalizes an input value to a JsonElement.
    /// </summary>
    /// <param name="input">The input value to normalize.</param>
    /// <returns>A JsonElement representation of the input.</returns>
    public static JsonElement NormalizeValue(object? input)
    {
        if (input == null)
        {
            return JsonDocument.Parse("null").RootElement;
        }

        // If already JsonElement, return as-is
        if (input is JsonElement element)
        {
            return element;
        }

        // Serialize to JSON and parse back to get JsonElement
        string json = JsonSerializer.Serialize(input, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
        
        return JsonDocument.Parse(json).RootElement;
    }

    /// <summary>
    /// Checks if a JsonElement is a primitive value.
    /// </summary>
    public static bool IsJsonPrimitive(JsonElement element)
    {
        return element.IsPrimitive();
    }

    /// <summary>
    /// Checks if a JsonElement is an array.
    /// </summary>
    public static bool IsJsonArray(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Array;
    }

    /// <summary>
    /// Checks if a JsonElement is an object.
    /// </summary>
    public static bool IsJsonObject(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Object;
    }

    /// <summary>
    /// Checks if a JsonElement array contains only primitive values.
    /// </summary>
    public static bool IsArrayOfPrimitives(JsonElement array)
    {
        if (!IsJsonArray(array))
            return false;

        return array.EnumerateArray().All(IsJsonPrimitive);
    }

    /// <summary>
    /// Checks if a JsonElement array contains only objects.
    /// </summary>
    public static bool IsArrayOfObjects(JsonElement array)
    {
        if (!IsJsonArray(array))
            return false;

        return array.EnumerateArray().All(IsJsonObject);
    }

    /// <summary>
    /// Checks if a JsonElement array contains only arrays.
    /// </summary>
    public static bool IsArrayOfArrays(JsonElement array)
    {
        if (!IsJsonArray(array))
            return false;

        return array.EnumerateArray().All(IsJsonArray);
    }
}