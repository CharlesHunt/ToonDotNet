using System.Text;
using System.Text.Json;

namespace ToonFormat;

public static partial class Toon
{
    // The buffer size used for StreamReader / StreamWriter throughout this file.
    private const int StreamBufferSize = 4096;

    // -------------------------------------------------------------------------
    // Synchronous stream operations
    // -------------------------------------------------------------------------

    /// <summary>
    /// Encodes <paramref name="input"/> to TOON format and writes it to
    /// <paramref name="stream"/>. The stream is left open after the call.
    /// </summary>
    /// <param name="input">The object to encode. Can be any serializable .NET object.</param>
    /// <param name="stream">The destination stream. Must be writable.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <param name="encoding">
    /// The text encoding to use. If null, <see cref="Encoding.UTF8"/> is used.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <example>
    /// <code>
    /// using var ms = new MemoryStream();
    /// Toon.Encode(data, ms);
    /// string toon = Encoding.UTF8.GetString(ms.ToArray());
    /// </code>
    /// </example>
    public static void Encode(object? input, Stream stream, EncodeOptions? options = null, Encoding? encoding = null)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));

        var toonString = Encode(input, options);
        WriteToStream(stream, toonString, encoding ?? Encoding.UTF8);
    }

    /// <summary>
    /// Reads TOON content from <paramref name="stream"/> and decodes it to a
    /// <see cref="JsonElement"/>. The stream is left open after the call.
    /// </summary>
    /// <param name="stream">The source stream. Must be readable.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <param name="encoding">
    /// The text encoding to use. If null, <see cref="Encoding.UTF8"/> is used.
    /// </param>
    /// <returns>A <see cref="JsonElement"/> representing the decoded data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the stream contains invalid TOON syntax.</exception>
    /// <example>
    /// <code>
    /// using var ms = new MemoryStream(Encoding.UTF8.GetBytes(toonString));
    /// JsonElement result = Toon.Decode(ms);
    /// </code>
    /// </example>
    public static JsonElement Decode(Stream stream, DecodeOptions? options = null, Encoding? encoding = null)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));

        var content = ReadFromStream(stream, encoding ?? Encoding.UTF8);
        return Decode(content, options);
    }

    /// <summary>
    /// Reads TOON content from <paramref name="stream"/> and deserializes it to a
    /// strongly-typed object of type <typeparamref name="T"/>. The stream is left open.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="stream">The source stream. Must be readable.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <param name="jsonOptions">Optional JSON serializer options for the deserialization step.</param>
    /// <param name="encoding">
    /// The text encoding to use. If null, <see cref="Encoding.UTF8"/> is used.
    /// </param>
    /// <returns>An instance of <typeparamref name="T"/> deserialized from the stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the decoded content cannot be converted to <typeparamref name="T"/>.</exception>
    /// <example>
    /// <code>
    /// using var ms = new MemoryStream(Encoding.UTF8.GetBytes(toonString));
    /// var result = Toon.Decode&lt;UserData&gt;(ms);
    /// </code>
    /// </example>
    public static T Decode<T>(Stream stream, DecodeOptions? options = null, JsonSerializerOptions? jsonOptions = null, Encoding? encoding = null)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));

        var content = ReadFromStream(stream, encoding ?? Encoding.UTF8);
        return Decode<T>(content, options, jsonOptions);
    }

    // -------------------------------------------------------------------------
    // Async stream operations
    // -------------------------------------------------------------------------

    /// <summary>
    /// Asynchronously encodes <paramref name="input"/> to TOON format and writes it
    /// to <paramref name="stream"/>. The stream is left open after the call.
    /// </summary>
    /// <param name="input">The object to encode. Can be any serializable .NET object.</param>
    /// <param name="stream">The destination stream. Must be writable.</param>
    /// <param name="options">Optional encoding options. If null, defaults are used.</param>
    /// <param name="encoding">
    /// The text encoding to use. If null, <see cref="Encoding.UTF8"/> is used.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the asynchronous write operation.</param>
    /// <example>
    /// <code>
    /// using var ms = new MemoryStream();
    /// await Toon.EncodeAsync(data, ms);
    /// string toon = Encoding.UTF8.GetString(ms.ToArray());
    /// </code>
    /// </example>
    public static async Task EncodeAsync(
        object? input,
        Stream stream,
        EncodeOptions? options = null,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));

        var toonString = Encode(input, options);
        await WriteToStreamAsync(stream, toonString, encoding ?? Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously reads TOON content from <paramref name="stream"/> and decodes
    /// it to a <see cref="JsonElement"/>. The stream is left open after the call.
    /// </summary>
    /// <param name="stream">The source stream. Must be readable.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <param name="encoding">
    /// The text encoding to use. If null, <see cref="Encoding.UTF8"/> is used.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the asynchronous read operation.</param>
    /// <returns>A <see cref="JsonElement"/> representing the decoded data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <example>
    /// <code>
    /// using var ms = new MemoryStream(Encoding.UTF8.GetBytes(toonString));
    /// JsonElement result = await Toon.DecodeAsync(ms);
    /// </code>
    /// </example>
    public static async Task<JsonElement> DecodeAsync(
        Stream stream,
        DecodeOptions? options = null,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));

        var content = await ReadFromStreamAsync(stream, encoding ?? Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        return Decode(content, options);
    }

    /// <summary>
    /// Asynchronously reads TOON content from <paramref name="stream"/> and deserializes
    /// it to a strongly-typed object of type <typeparamref name="T"/>. The stream is left open.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="stream">The source stream. Must be readable.</param>
    /// <param name="options">Optional decoding options. If null, defaults are used.</param>
    /// <param name="jsonOptions">Optional JSON serializer options for the deserialization step.</param>
    /// <param name="encoding">
    /// The text encoding to use. If null, <see cref="Encoding.UTF8"/> is used.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the asynchronous read operation.</param>
    /// <returns>An instance of <typeparamref name="T"/> deserialized from the stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the decoded content cannot be converted to <typeparamref name="T"/>.</exception>
    /// <example>
    /// <code>
    /// using var ms = new MemoryStream(Encoding.UTF8.GetBytes(toonString));
    /// var result = await Toon.DecodeAsync&lt;UserData&gt;(ms);
    /// </code>
    /// </example>
    public static async Task<T> DecodeAsync<T>(
        Stream stream,
        DecodeOptions? options = null,
        JsonSerializerOptions? jsonOptions = null,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));

        var content = await ReadFromStreamAsync(stream, encoding ?? Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        return Decode<T>(content, options, jsonOptions);
    }

    // -------------------------------------------------------------------------
    // Private stream helpers
    // -------------------------------------------------------------------------

    private static string ReadFromStream(Stream stream, Encoding encoding)
    {
        using var reader = new StreamReader(stream, encoding,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: StreamBufferSize,
            leaveOpen: true);
        return reader.ReadToEnd();
    }

    private static void WriteToStream(Stream stream, string content, Encoding encoding)
    {
        using var writer = new StreamWriter(stream, encoding,
            bufferSize: StreamBufferSize,
            leaveOpen: true);
        writer.Write(content);
        // Flush is called by Dispose; explicit call ensures the buffer is
        // written before the caller reads the stream position.
        writer.Flush();
    }

#if NETSTANDARD2_0
    private static async Task<string> ReadFromStreamAsync(Stream stream, Encoding encoding, CancellationToken _)
    {
        using var reader = new StreamReader(stream, encoding,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: StreamBufferSize,
            leaveOpen: true);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    private static async Task WriteToStreamAsync(Stream stream, string content, Encoding encoding, CancellationToken _)
    {
        var writer = new StreamWriter(stream, encoding,
            bufferSize: StreamBufferSize,
            leaveOpen: true);
        try
        {
            await writer.WriteAsync(content).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            writer.Dispose();
        }
    }
#else
    private static async Task<string> ReadFromStreamAsync(Stream stream, Encoding encoding, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, encoding,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: StreamBufferSize,
            leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteToStreamAsync(Stream stream, string content, Encoding encoding, CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(stream, encoding,
            bufferSize: StreamBufferSize,
            leaveOpen: true);
        await writer.WriteAsync(content.AsMemory(), cancellationToken).ConfigureAwait(false);
    }
#endif
}
