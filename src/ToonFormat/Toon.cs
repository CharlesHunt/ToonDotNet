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
    /// Serializes the specified input object and saves it to a file in TOON format at the given path using the provided encoding
    /// options.
    /// </summary>
    /// <param name="input">The object to serialize and save. If null, an empty representation will be written to the file.</param>
    /// <param name="filePath">The path of the file to which the serialized data will be written. Cannot be null or empty.</param>
    /// <param name="encodeOptions">Optional encoding options that control how the input object is serialized. If null, default encoding options are
    /// used.</param>
    public static void Save(object? input, string filePath, EncodeOptions? encodeOptions = null)
    {
        var toonString = Encode(input, encodeOptions);
        System.IO.File.WriteAllText(filePath, toonString);
    }

    /// <summary>
    /// Loads and decodes a JSON element from the specified file using optional decoding options.
    /// </summary>
    /// <param name="filePath">The path to the file containing the JSON data to load. Cannot be null or empty.</param>
    /// <param name="decodeOptions">Optional decoding options that influence how the JSON data is interpreted. If null, default decoding behavior is
    /// used.</param>
    /// <returns>A <see cref="JsonElement"/> representing the decoded JSON data from the file.</returns>
    public static JsonElement Load(string filePath, DecodeOptions? decodeOptions = null)
    {
        var toonString = System.IO.File.ReadAllText(filePath);
        var result = Decode(toonString, decodeOptions);
        return result;
    }

    /// <summary>
    /// Deserializes an object of type T from a TOON file at the specified path, using optional decoding and serializer
    /// options.
    /// </summary>
    /// <remarks>This method reads the entire contents of the file at <paramref name="filePath"/> and attempts
    /// to decode it into an object of type T. If the file does not exist or contains invalid TOON, an exception may be
    /// thrown. The decoding and serializer options allow customization of the deserialization process.</remarks>
    /// <typeparam name="T">The type of object to deserialize from the TOON file.</typeparam>
    /// <param name="filePath">The path to the TOON file to load and deserialize. Cannot be null or empty.</param>
    /// <param name="decodeOptions">Optional decoding options that influence how the TOON content is interpreted. If null, default decoding behavior
    /// is used.</param>
    /// <param name="jsonOptions">Optional serializer options that control JSON deserialization settings. If null, default serializer options are
    /// applied.</param>
    /// <returns>An instance of type T deserialized from the specified TON file.</returns>
    public static T Load<T>(string filePath, DecodeOptions? decodeOptions = null, JsonSerializerOptions? jsonOptions = null)
    {
        var toonString = System.IO.File.ReadAllText(filePath);
        var result = Decode<T>(toonString, decodeOptions, jsonOptions);
        return result;
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

    /// <summary>
    /// Converts a JSON string directly to TOON format.
    /// This is the most efficient method for JSON-to-TOON conversion as it avoids
    /// intermediate object serialization.
    /// </summary>
    /// <param name="jsonString">A valid JSON string to convert to TOON format.</param>
    /// <param name="options">Optional encoding options. If null, default options are used.</param>
    /// <returns>A TOON format string representation of the JSON input.</returns>
    /// <exception cref="ArgumentException">Thrown when jsonString is null or empty.</exception>
    /// <exception cref="JsonException">Thrown when jsonString is not valid JSON.</exception>
    /// <example>
    /// <code>
    /// string json = "{\"users\":[{\"id\":1,\"name\":\"Alice\",\"role\":\"admin\"},{\"id\":2,\"name\":\"Bob\",\"role\":\"user\"}]}";
    /// string toonString = Toon.FromJson(json);
    /// // Result: users[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user
    /// </code>
    /// </example>
    public static string FromJson(string jsonString, EncodeOptions? options = null)
    {
        if (string.IsNullOrEmpty(jsonString))
        {
            throw new ArgumentException("JSON string cannot be null or empty", nameof(jsonString));
        }

        JsonElement jsonElement;
        try
        {
            jsonElement = JsonDocument.Parse(jsonString).RootElement;
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Invalid JSON string: {ex.Message}", ex);
        }

        var resolvedOptions = options ?? new EncodeOptions();
        return ToonEncoder.EncodeValue(jsonElement, resolvedOptions);
    }

    /// <summary>
    /// Converts a JSON file directly to TOON format.
    /// This method reads a JSON file and converts it to TOON format efficiently.
    /// </summary>
    /// <param name="jsonFilePath">The path to the JSON file to convert.</param>
    /// <param name="options">Optional encoding options. If null, default options are used.</param>
    /// <returns>A TOON format string representation of the JSON file contents.</returns>
    /// <exception cref="ArgumentException">Thrown when jsonFilePath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="JsonException">Thrown when the file does not contain valid JSON.</exception>
    /// <example>
    /// <code>
    /// string toonString = Toon.FromJsonFile("data.json");
    /// // Converts the JSON file to TOON format
    /// </code>
    /// </example>
    public static string FromJsonFile(string jsonFilePath, EncodeOptions? options = null)
    {
        if (string.IsNullOrEmpty(jsonFilePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(jsonFilePath));
        }

        if (!System.IO.File.Exists(jsonFilePath))
        {
            throw new System.IO.FileNotFoundException($"JSON file not found: {jsonFilePath}", jsonFilePath);
        }

        string jsonString = System.IO.File.ReadAllText(jsonFilePath);
        return FromJson(jsonString, options);
    }

    /// <summary>
    /// Converts a TOON format string directly to JSON.
    /// This is the most efficient method for TOON-to-JSON conversion as it produces
    /// compact JSON output from the decoded TOON data.
    /// </summary>
    /// <param name="toonString">A valid TOON format string to convert to JSON.</param>
    /// <param name="decodeOptions">Optional decoding options. If null, default options are used.</param>
    /// <param name="jsonOptions">Optional JSON serialization options. If null, compact JSON is produced.</param>
    /// <returns>A JSON string representation of the TOON input.</returns>
    /// <exception cref="ArgumentException">Thrown when toonString is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when toonString contains invalid TOON syntax.</exception>
    /// <example>
    /// <code>
    /// string toon = "users[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user";
    /// string json = Toon.ToJson(toon);
    /// // Result: {"users":[{"id":1,"name":"Alice","role":"admin"},{"id":2,"name":"Bob","role":"user"}]}
    /// </code>
    /// </example>
    public static string ToJson(string toonString, DecodeOptions? decodeOptions = null, JsonSerializerOptions? jsonOptions = null)
    {
        if (string.IsNullOrEmpty(toonString))
        {
            throw new ArgumentException("TOON string cannot be null or empty", nameof(toonString));
        }

        var jsonElement = Decode(toonString, decodeOptions);
        
        var serializerOptions = jsonOptions ?? new JsonSerializerOptions
        {
            WriteIndented = false
        };

        return JsonSerializer.Serialize(jsonElement, serializerOptions);
    }

    /// <summary>
    /// Converts a TOON format file directly to JSON.
    /// This method reads a TOON file and converts it to JSON format efficiently.
    /// </summary>
    /// <param name="toonFilePath">The path to the TOON file to convert.</param>
    /// <param name="decodeOptions">Optional decoding options. If null, default options are used.</param>
    /// <param name="jsonOptions">Optional JSON serialization options. If null, compact JSON is produced.</param>
    /// <returns>A JSON string representation of the TOON file contents.</returns>
    /// <exception cref="ArgumentException">Thrown when toonFilePath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the file does not contain valid TOON syntax.</exception>
    /// <example>
    /// <code>
    /// string json = Toon.ToJsonFile("data.toon");
    /// // Converts the TOON file to JSON format
    /// </code>
    /// </example>
    public static string ToJsonFile(string toonFilePath, DecodeOptions? decodeOptions = null, JsonSerializerOptions? jsonOptions = null)
    {
        if (string.IsNullOrEmpty(toonFilePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(toonFilePath));
        }

        if (!System.IO.File.Exists(toonFilePath))
        {
            throw new System.IO.FileNotFoundException($"TOON file not found: {toonFilePath}", toonFilePath);
        }

        string toonString = System.IO.File.ReadAllText(toonFilePath);
        return ToJson(toonString, decodeOptions, jsonOptions);
    }

    /// <summary>
    /// Converts a TOON format string to JSON and saves it to a file.
    /// </summary>
    /// <param name="toonString">A valid TOON format string to convert.</param>
    /// <param name="jsonFilePath">The path where the JSON file will be saved.</param>
    /// <param name="decodeOptions">Optional decoding options. If null, default options are used.</param>
    /// <param name="jsonOptions">Optional JSON serialization options. If null, indented JSON is produced for readability.</param>
    /// <exception cref="ArgumentException">Thrown when toonString or jsonFilePath is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when toonString contains invalid TOON syntax.</exception>
    /// <example>
    /// <code>
    /// string toon = "users[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user";
    /// Toon.ToJsonFile(toon, "output.json");
    /// // Creates output.json with formatted JSON
    /// </code>
    /// </example>
    public static void SaveAsJson(string toonString, string jsonFilePath, DecodeOptions? decodeOptions = null, JsonSerializerOptions? jsonOptions = null)
    {
        if (string.IsNullOrEmpty(toonString))
        {
            throw new ArgumentException("TOON string cannot be null or empty", nameof(toonString));
        }

        if (string.IsNullOrEmpty(jsonFilePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(jsonFilePath));
        }

        var serializerOptions = jsonOptions ?? new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = ToJson(toonString, decodeOptions, serializerOptions);
        System.IO.File.WriteAllText(jsonFilePath, json);
    }
}