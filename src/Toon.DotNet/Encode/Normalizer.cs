using System.Text.Json;
using System.Text.Json.Serialization;

namespace ToonFormat.Encode;

/// <summary>
/// Utilities for normalizing input values to JsonElement for encoding.
/// </summary>
internal static class Normalizer
{
    private static readonly JsonSerializerOptions NormalizeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new NonFiniteDoubleConverter(), new NonFiniteSingleConverter() }
    };

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
        string json = JsonSerializer.Serialize(input, NormalizeOptions);

        return JsonDocument.Parse(json).RootElement;
    }

    /// <summary>
    /// Writes NaN/+Infinity/-Infinity as JSON null (spec §3) instead of
    /// System.Text.Json's default behavior of throwing. Named-literal
    /// number handling isn't used here because it writes non-standard JSON
    /// tokens (bare "NaN"/"Infinity") that JsonDocument.Parse cannot read
    /// back.
    /// </summary>
    private sealed class NonFiniteDoubleConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetDouble();

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteNumberValue(value);
            }
        }
    }

    /// <summary>
    /// Single-precision counterpart of <see cref="NonFiniteDoubleConverter"/>.
    /// </summary>
    private sealed class NonFiniteSingleConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetSingle();

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteNumberValue(value);
            }
        }
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