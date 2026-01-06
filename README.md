# Toon Serializer for .NET

[![NuGet Downloads](https://img.shields.io/nuget/dt/Toon.DotNet.svg?label=Downloads&color=green)](https://www.nuget.org/packages/Toon.DotNet)
[![Tests](https://img.shields.io/badge/tests-passing-brightgreen.svg)](https://github.com/CharlesHunt/ToonDotNet/actions)
[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-blue.svg)![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blue.svg)![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download)
[![NetStandard2.0](https://img.shields.io/badge/NetStandard2.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
---
Token-Oriented Object Notation (TOON) Serializer — a compact, human-readable serialization format designed for passing structured data to Large Language Models with significantly reduced token usage. TOON shines for uniform arrays of objects and readable nested structures. Optimised for .Net 10.0 plus backwards compatible with earlier versions. 

- Token-efficient alternative to JSON for LLM prompts
- Human-friendly and diff-friendly
- Strongly-typed decode support via System.Text.Json
- Strict validation options and round-trip helpers
- Direct JSON-to-TOON and TOON-to-JSON conversion methods for seamless interoperability.
- File operations for reading and writing TOON and JSON data.
- Examples included. 
- Unit tests with high code coverage. 
- 90 unit tests 100% passing.

---
**Targets:** 
.NET Standard 2.0 - maximum compatibility.
.NET 10 - dependency free.

---
## License
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

---
## How it works
![TOON logo with step‑by‑step guide](./og.png)
---
## Installation

Install from NuGet:

```
dotnet add package Toon.DotNet
```
---
## Compatibility
- .Net8.0
- .Net9.0
- .Net10.0 
- .Net Standard 2.0 (.NET Framework 4.6.1+, Mono and Unity)

---
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
---
## API overview

#### Basic Serilialization methods
- `Toon.Encode(object value, EncodeOptions? options = null)`
- `Toon.Decode(string input, DecodeOptions? options = null)` → `JsonElement`
- `Toon.Decode<T>(string input, DecodeOptions? options = null, JsonSerializerOptions? jsonOptions = null)`
#### Json Conversion methods
- `Toon.FromJson(string jsonString, EncodeOptions? options = null)` - **Efficient JSON-to-TOON conversion**
- `Toon.FromJsonFile(string jsonFilePath, EncodeOptions? options = null)` - **Convert JSON files to TOON**
- `Toon.ToJson(string toonString, DecodeOptions? decodeOptions = null, JsonSerializerOptions? jsonOptions = null)` - **Efficient TOON-to-JSON conversion**
- `Toon.ToJsonFile(string toonFilePath, DecodeOptions? decodeOptions = null, JsonSerializerOptions? jsonOptions = null)` - **Convert TOON files to JSON**
- `Toon.SaveAsJson(string toonString, string jsonFilePath, DecodeOptions? decodeOptions = null, JsonSerializerOptions? jsonOptions = null)` - **Save TOON as JSON file**
#### Validation and Utilities
- `Toon.IsValid(string input, DecodeOptions? options = null)`
- `Toon.RoundTrip(object value, EncodeOptions? encodeOptions = null, DecodeOptions? decodeOptions = null)`
- `Toon.SizeComparisonPercentage<T>(T input, EncodeOptions? encodeOptions = null)`
#### File operations
- `Toon.Save(object? value, string filePath, EncodeOptions? options = null)`
- `Toon.Load<T>(string filePath, DecodeOptions? options = null, JsonSerializerOptions? jsonOptions = null)`
- `Toon.Load(string filePath, DecodeOptions? options = null)`

---
### JSON to TOON Conversion

The most efficient way to convert JSON to TOON format:

**Why use `FromJson`?**
- **More efficient**: Parses JSON directly to TOON without intermediate object creation
- **Memory efficient**: Single parse operation with minimal allocations
- **Faster**: Bypasses object serialization/deserialization overhead
- **Flexible**: Works with any valid JSON string or file

---
### TOON to JSON Conversion

The most efficient way to convert TOON format back to JSON:

**Why use `ToJson`?**
- **Efficient**: Direct TOON decoding to JSON string
- **Flexible output**: Control JSON formatting (compact or indented)
- **Interoperability**: Easy integration with systems that require JSON
- **Bidirectional**: Perfect complement to `FromJson` for round-trip conversions

---
### Options

`EncodeOptions`
- `Indent` — spaces per level (default:2)
- `Delimiter` — value delimiter for rows/inline arrays (default: ',')
- `LengthMarker` — optional array length marker (e.g. '#')

`DecodeOptions`
- `Indent` — expected spaces per level (default:2)
- `Strict` — validate lengths/row counts and forbid stray blank lines

---
### Customization example

```csharp
var opts = new EncodeOptions {
 Indent =4,
 Delimiter = '|',
 LengthMarker = '#'
};
var toon = Toon.Encode(data, opts);
```

---
### Size comparison example

```csharp
// original data
var data = new[] {
 new { id =1, name = "Alice", role = "admin" },
 new { id =2, name = "Bob", role = "user" },
 new { id =3, name = "Charlie", role = "user" },
 new { id =4, name = "Dana", role = "admin" },
};

// encode with custom options
var encodeOptions = new EncodeOptions {  Indent = 2, Delimiter = '|' };
var toon = Toon.Encode(data, encodeOptions);
// => id|name|role
//   -+----+------+
//    1|Alice|admin |
//    2| Bob | user |
//    3|Charlie| user |
//    4| Dana |admin |

// get size comparison percentage
var pct = Toon.SizeComparisonPercentage(data, encodeOptions);
// ⇒ 28.57 (TOON is ~28.57% smaller than JSON for this data)

```
---
## When to use TOON

- Uniform arrays of objects (tabular data)
- Human-readable prompt payloads for LLMs
- Compact, copy/paste friendly format with stable structure

For deeply nested, highly irregular data, plain JSON may be more compact.

---
## Package Dependencies

- System.Text.Json (part of .NET)
- Microsoft.SourceLink.GitHub (for source linking in PDBs)
- NetStandard.Library (for .NET Standard 2.0 compatibility)

---
## Samples

See `examples/ToonFormat.Example` for a runnable console sample.

---
## Versioning

This project follows semantic versioning. See [`CHANGELOG.md`](./CHANGELOG.md) for release notes.

---
## Contributing

Contributions are welcome. See [`CONTRIBUTING.md`](./CONTRIBUTING.md) and [`CODE_OF_CONDUCT.md`](./CODE_OF_CONDUCT.md).

---
## Security

Please see [`SECURITY.md`](./SECURITY.md) for reporting vulnerabilities.

---
## License

MIT License — see [`LICENSE`](./LICENSE).
