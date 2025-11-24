# ToonFormat for .NET

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blue.svg)![.NET 10.0](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download)[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

Token-Oriented Object Notation (TOON) — a compact, human-readable serialization format designed for passing structured data to Large Language Models with significantly reduced token usage. TOON shines for uniform arrays of objects and readable nested structures.

- Token-efficient alternative to JSON for LLM prompts
- Human-friendly and diff-friendly
- Strongly-typed decode support via System.Text.Json
- Strict validation options and round-trip helpers

![TOON logo with step‑by‑step guide](./og.png)

## Installation

Install from NuGet:

```
dotnet add package Toon.DotNet
```

Target Frameworks: net8.0, net9.0, net10.0

## Quick start

```csharp
using ToonFormat;

var data = new {
 users = new[] {
 new { id =1, name = "Alice", role = "admin" },
 new { id =2, name = "Bob", role = "user" }
 }
};

// Encode to TOON
string toon = Toon.Encode(data);
// users[2]{id,name,role}:
//1,Alice,admin
//2,Bob,user

// Decode to JsonElement
var json = Toon.Decode(toon);

// Decode to a typed model
var users = Toon.Decode<UserData>(toon);

public class UserData { public User[] Users { get; set; } = Array.Empty<User>(); }
public class User { public int Id { get; set; } public string Name { get; set; } = ""; public string Role { get; set; } = ""; }
```

## API overview

- `Toon.Encode(object value, EncodeOptions? options = null)`
- `Toon.Decode(string input, DecodeOptions? options = null)` → `JsonElement`
- `Toon.Decode<T>(string input, DecodeOptions? options = null, JsonSerializerOptions? jsonOptions = null)`
- `Toon.IsValid(string input, DecodeOptions? options = null)`
- `Toon.RoundTrip(object value, EncodeOptions? encodeOptions = null, DecodeOptions? decodeOptions = null)`

### Options

`EncodeOptions`
- `Indent` — spaces per level (default:2)
- `Delimiter` — value delimiter for rows/inline arrays (default: ',')
- `LengthMarker` — optional array length marker (e.g. '#')

`DecodeOptions`
- `Indent` — expected spaces per level (default:2)
- `Strict` — validate lengths/row counts and forbid stray blank lines

### Customization example

```csharp
var opts = new EncodeOptions {
 Indent =4,
 Delimiter = '|',
 LengthMarker = '#'
};
var toon = Toon.Encode(data, opts);
```

## When to use TOON

- Uniform arrays of objects (tabular data)
- Human-readable prompt payloads for LLMs
- Compact, copy/paste friendly format with stable structure

For deeply nested, highly irregular data, plain JSON may be more compact.

## Samples

See `examples/ToonFormat.Example` for a runnable console sample.

## Versioning

This project follows semantic versioning. See [`CHANGELOG.md`](./CHANGELOG.md) for release notes.

## Contributing

Contributions are welcome. See [`CONTRIBUTING.md`](./CONTRIBUTING.md) and [`CODE_OF_CONDUCT.md`](./CODE_OF_CONDUCT.md).

## Security

Please see [`SECURITY.md`](./SECURITY.md) for reporting vulnerabilities.

## License

MIT License — see [`LICENSE`](./LICENSE).
