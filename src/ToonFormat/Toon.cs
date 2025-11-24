using System.Text.Json;
using ToonFormat.Decode;
using ToonFormat.Encode;

namespace ToonFormat;

/// <summary>
/// Main class for encoding and decoding TOON (Token-Oriented Object Notation) format.
/// TOON is a compact, human-readable serialization format designed for passing structured data 
/// to Large Language Models with significantly reduced token usage.
/// </summary>
public static class Toon
{
    /// <summary>
    /// Encodes an object to TOON format string.
    /// </summary>
    /// <param name="input">The object to encode. Can be any serializable .NET object.</param>
    /// <param name="options">Optional encoding options. If null, default options are used.</param>
    /// <returns>A TOON format string representation of the input object.</returns>
    /// <example>
    /// <code>
    /// var data = new { users = new[] { 
    ///     new { id = 1, name = "Alice", role = "admin" },
    ///     new { id = 2, name = "Bob", role = "user" }
    /// }};
    /// string toonString = Toon.Encode(data);
    /// // Result: users[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user
    /// </code>
    /// </example>
    public static string Encode(object? input, EncodeOptions? options = null)
    {
        var normalizedValue = Normalizer.NormalizeValue(input);
        var resolvedOptions = options ?? new EncodeOptions();
        return ToonEncoder.EncodeValue(normalizedValue, resolvedOptions);
    }

    /// <summary>
    /// Decodes a TOON format string to a JsonElement.
    /// </summary>
    /// <param name="input">The TOON format string to decode.</param>
    /// <param name="options">Optional decoding options. If null, default options are used.</param>
    /// <returns>A JsonElement representing the decoded data structure.</returns>
    /// <exception cref="ArgumentException">Thrown when input is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when input contains invalid TOON syntax.</exception>
    /// <example>
    /// <code>
    /// string toonString = "users[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user";
    /// JsonElement result = Toon.Decode(toonString);
    /// // Access the data: result.GetProperty("users")[0].GetProperty("name").GetString() == "Alice"
    /// </code>
    /// </example>
    public static JsonElement Decode(string input, DecodeOptions? options = null)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("Input cannot be null or empty", nameof(input));
        }

        var resolvedOptions = options ?? new DecodeOptions();
        return ToonDecoder.DecodeValue(input, resolvedOptions);
    }

    /// <summary>
    /// Decodes a TOON format string to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="input">The TOON format string to decode.</param>
    /// <param name="options">Optional decoding options. If null, default options are used.</param>
    /// <param name="jsonOptions">Optional JSON serialization options for the final deserialization step.</param>
    /// <returns>An object of type T representing the decoded data.</returns>
    /// <exception cref="ArgumentException">Thrown when input is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when input contains invalid TOON syntax.</exception>
    /// <exception cref="JsonException">Thrown when the decoded JsonElement cannot be converted to type T.</exception>
    /// <example>
    /// <code>
    /// public class User { public int Id { get; set; } public string Name { get; set; } public string Role { get; set; } }
    /// public class UserData { public User[] Users { get; set; } }
    /// 
    /// string toonString = "users[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user";
    /// UserData result = Toon.Decode&lt;UserData&gt;(toonString);
    /// // Access the data: result.Users[0].Name == "Alice"
    /// </code>
    /// </example>
    public static T Decode<T>(string input, DecodeOptions? options = null, JsonSerializerOptions? jsonOptions = null)
    {
        var jsonElement = Decode(input, options);
        var jsonString = jsonElement.GetRawText();
        
        var serializerOptions = jsonOptions ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var result = JsonSerializer.Deserialize<T>(jsonString, serializerOptions);
        return result ?? throw new JsonException($"Failed to deserialize to type {typeof(T).Name}");
    }

    /// <summary>
    /// Validates that a TOON format string is syntactically correct.
    /// </summary>
    /// <param name="input">The TOON format string to validate.</param>
    /// <param name="options">Optional decoding options for validation. If null, default options are used.</param>
    /// <returns>True if the input is valid TOON format, false otherwise.</returns>
    /// <example>
    /// <code>
    /// bool isValid = Toon.IsValid("users[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user");
    /// // isValid == true
    /// </code>
    /// </example>
    public static bool IsValid(string input, DecodeOptions? options = null)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        try
        {
            Decode(input, options);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Performs a round-trip test: encodes an object to TOON format, then decodes it back.
    /// This is useful for testing data fidelity and format compatibility.
    /// </summary>
    /// <param name="input">The object to test.</param>
    /// <param name="encodeOptions">Optional encoding options.</param>
    /// <param name="decodeOptions">Optional decoding options.</param>
    /// <returns>The decoded JsonElement after the round-trip.</returns>
    /// <example>
    /// <code>
    /// var originalData = new { value = 1.0 / 3.0 }; // 0.3333333333333333
    /// JsonElement roundTripResult = Toon.RoundTrip(originalData);
    /// // Verify: roundTripResult.GetProperty("value").GetDouble() == originalData.value
    /// </code>
    /// </example>
    public static JsonElement RoundTrip(object? input, EncodeOptions? encodeOptions = null, DecodeOptions? decodeOptions = null)
    {
        string encoded = Encode(input, encodeOptions);
        return Decode(encoded, decodeOptions);
    }

    /// <summary>
    /// Compares the size of the TOON format string to the JSON string representation of the input object.
    /// Returns the percentage reduction in size when using TOON format compared to JSON.
    /// </summary>
    /// <typeparam name="T">The type of the input object.</typeparam>
    /// <param name="input">The object to compare.</param>
    /// <param name="encodeOptions">Optional encoding options for TOON format.</param>
    /// <returns>
    /// The percentage reduction in size (rounded to two decimal places) of the TOON format string compared to the JSON string.
    /// Returns 0 if the JSON string length is zero.
    /// </returns>
    public static decimal SizeComparisonPercentage<T>(T input, EncodeOptions? encodeOptions = null)
    {
        var jsonString = JsonSerializer.Serialize(input);
        var toonString = Encode(input, encodeOptions);
        if (jsonString.Length == 0)
            return 0m;
        decimal percentage = 100m - ((decimal)toonString.Length * 100m / (decimal)jsonString.Length);
        return Math.Round(percentage, 2);
    }
}