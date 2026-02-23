using System.Text.Json;

namespace ToonFormat;

public static partial class Toon
{
    // -------------------------------------------------------------------------
    // Private file-I/O helpers
    // File.ReadAllTextAsync / File.WriteAllTextAsync require netstandard2.1+,
    // so on netstandard2.0 we fall back to StreamReader / StreamWriter which
    // have been async since netstandard2.0. The CancellationToken parameter is
    // accepted on all targets for a consistent public API, but is only forwarded
    // on .NET 8+ where the BCL overloads support it.
    // -------------------------------------------------------------------------

#if NETSTANDARD2_0
    private static async Task<string> ReadFileAsync(string path, CancellationToken _)
    {
        using var reader = new System.IO.StreamReader(path);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    private static async Task WriteFileAsync(string path, string content, CancellationToken _)
    {
        using var writer = new System.IO.StreamWriter(path, append: false);
        await writer.WriteAsync(content).ConfigureAwait(false);
    }
#else
    private static Task<string> ReadFileAsync(string path, CancellationToken cancellationToken) =>
        System.IO.File.ReadAllTextAsync(path, cancellationToken);

    private static Task WriteFileAsync(string path, string content, CancellationToken cancellationToken) =>
        System.IO.File.WriteAllTextAsync(path, content, cancellationToken);
#endif

    // -------------------------------------------------------------------------
    // Async file operations
    // -------------------------------------------------------------------------

    /// <summary>
    /// Asynchronously serializes <paramref name="input"/> to TOON format and writes
    /// it to the file at <paramref name="filePath"/>, creating or overwriting the file.
    /// </summary>
    /// <param name="input">The object to serialize. If null, an empty representation is written.</param>
    /// <param name="filePath">Destination file path. Cannot be null or empty.</param>
    /// <param name="encodeOptions">Optional encoding options. If null, defaults are used.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <example>
    /// <code>
    /// await Toon.SaveAsync(data, "output.toon");
    /// </code>
    /// </example>
    public static async Task SaveAsync(
        object? input,
        string filePath,
        EncodeOptions? encodeOptions = null,
        CancellationToken cancellationToken = default)
    {
        var toonString = Encode(input, encodeOptions);
        await WriteFileAsync(filePath, toonString, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously reads a TOON file and decodes it to a <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="filePath">Path to the TOON file to read.</param>
    /// <param name="decodeOptions">Optional decoding options. If null, defaults are used.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="JsonElement"/> representing the decoded data.</returns>
    /// <example>
    /// <code>
    /// JsonElement result = await Toon.LoadAsync("data.toon");
    /// </code>
    /// </example>
    public static async Task<JsonElement> LoadAsync(
        string filePath,
        DecodeOptions? decodeOptions = null,
        CancellationToken cancellationToken = default)
    {
        var toonString = await ReadFileAsync(filePath, cancellationToken).ConfigureAwait(false);
        return Decode(toonString, decodeOptions);
    }

    /// <summary>
    /// Asynchronously reads a TOON file and deserializes it to a strongly-typed object of type
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="filePath">Path to the TOON file to read.</param>
    /// <param name="decodeOptions">Optional decoding options. If null, defaults are used.</param>
    /// <param name="jsonOptions">Optional JSON serializer options for the final deserialization step.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>An instance of <typeparamref name="T"/> deserialized from the file.</returns>
    /// <exception cref="JsonException">Thrown when the decoded content cannot be converted to <typeparamref name="T"/>.</exception>
    /// <example>
    /// <code>
    /// var users = await Toon.LoadAsync&lt;UserData&gt;("data.toon");
    /// </code>
    /// </example>
    public static async Task<T> LoadAsync<T>(
        string filePath,
        DecodeOptions? decodeOptions = null,
        JsonSerializerOptions? jsonOptions = null,
        CancellationToken cancellationToken = default)
    {
        var toonString = await ReadFileAsync(filePath, cancellationToken).ConfigureAwait(false);
        return Decode<T>(toonString, decodeOptions, jsonOptions);
    }

    /// <summary>
    /// Asynchronously reads a JSON file and converts its contents to TOON format.
    /// </summary>
    /// <param name="jsonFilePath">Path to the source JSON file.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A TOON format string representing the JSON file contents.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="jsonFilePath"/> is null or empty.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="JsonException">Thrown when the file does not contain valid JSON.</exception>
    /// <example>
    /// <code>
    /// string toon = await Toon.FromJsonFileAsync("data.json");
    /// </code>
    /// </example>
    public static async Task<string> FromJsonFileAsync(
        string jsonFilePath,
        EncodeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(jsonFilePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(jsonFilePath));

        if (!System.IO.File.Exists(jsonFilePath))
            throw new System.IO.FileNotFoundException($"JSON file not found: {jsonFilePath}", jsonFilePath);

        var jsonString = await ReadFileAsync(jsonFilePath, cancellationToken).ConfigureAwait(false);
        return FromJson(jsonString, options);
    }

    /// <summary>
    /// Asynchronously reads a TOON file and converts its contents to a JSON string.
    /// </summary>
    /// <param name="toonFilePath">Path to the source TOON file.</param>
    /// <param name="decodeOptions">Optional decoding options. If null, defaults are used.</param>
    /// <param name="jsonOptions">Optional JSON serializer options. If null, compact JSON is produced.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A JSON string representing the TOON file contents.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="toonFilePath"/> is null or empty.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the file contains invalid TOON syntax.</exception>
    /// <example>
    /// <code>
    /// string json = await Toon.ToJsonFileAsync("data.toon");
    /// </code>
    /// </example>
    public static async Task<string> ToJsonFileAsync(
        string toonFilePath,
        DecodeOptions? decodeOptions = null,
        JsonSerializerOptions? jsonOptions = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(toonFilePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(toonFilePath));

        if (!System.IO.File.Exists(toonFilePath))
            throw new System.IO.FileNotFoundException($"TOON file not found: {toonFilePath}", toonFilePath);

        var toonString = await ReadFileAsync(toonFilePath, cancellationToken).ConfigureAwait(false);
        return ToJson(toonString, decodeOptions, jsonOptions);
    }

    /// <summary>
    /// Asynchronously decodes a TOON string to JSON and writes the result to a file.
    /// </summary>
    /// <param name="toonString">A valid TOON format string to convert.</param>
    /// <param name="jsonFilePath">Destination file path for the JSON output.</param>
    /// <param name="decodeOptions">Optional decoding options. If null, defaults are used.</param>
    /// <param name="jsonOptions">Optional JSON serializer options. If null, indented JSON is written.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="toonString"/> or <paramref name="jsonFilePath"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="toonString"/> contains invalid TOON syntax.</exception>
    /// <example>
    /// <code>
    /// await Toon.SaveAsJsonAsync(toon, "output.json");
    /// </code>
    /// </example>
    public static async Task SaveAsJsonAsync(
        string toonString,
        string jsonFilePath,
        DecodeOptions? decodeOptions = null,
        JsonSerializerOptions? jsonOptions = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(toonString))
            throw new ArgumentException("TOON string cannot be null or empty", nameof(toonString));

        if (string.IsNullOrEmpty(jsonFilePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(jsonFilePath));

        var serializerOptions = jsonOptions ?? new JsonSerializerOptions { WriteIndented = true };
        var json = ToJson(toonString, decodeOptions, serializerOptions);
        await WriteFileAsync(jsonFilePath, json, cancellationToken).ConfigureAwait(false);
    }
}
