# ToonFormat for .NET

A .NET implementation of the TOON (Token-Oriented Object Notation) format - a compact, human-readable serialization format designed for passing structured data to Large Language Models with significantly reduced token usage.

## Features

- **Token Efficient**: Significantly reduces token usage compared to JSON for LLM prompts
- **Human Readable**: Easy to read and write manually 
- **Tabular Format**: Optimized for uniform arrays of objects
- **Strong Types**: Full .NET type support with JsonElement integration
- **Validation**: Strict parsing with comprehensive error reporting

## Installation

```bash
dotnet add package ToonFormat
```

## Quick Start

```csharp
using ToonFormat;

// Encoding objects to TOON format
var data = new { 
    users = new[] { 
        new { id = 1, name = "Alice", role = "admin" },
        new { id = 2, name = "Bob", role = "user" }
    }
};

string toonString = Toon.Encode(data);
Console.WriteLine(toonString);
// Output:
// users[2]{id,name,role}:
//   1,Alice,admin  
//   2,Bob,user

// Decoding TOON format back to objects
var decoded = Toon.Decode<dynamic>(toonString);
```

## API Reference

### Main Methods

- `Toon.Encode(object, EncodeOptions?)` - Encode object to TOON string
- `Toon.Decode(string, DecodeOptions?)` - Decode TOON string to JsonElement  
- `Toon.Decode<T>(string, DecodeOptions?, JsonSerializerOptions?)` - Decode to strongly-typed object
- `Toon.IsValid(string, DecodeOptions?)` - Validate TOON format syntax
- `Toon.RoundTrip(object, EncodeOptions?, DecodeOptions?)` - Test encoding/decoding fidelity

### Configuration Options

**EncodeOptions:**
- `Indent` - Spaces per indentation level (default: 2)  
- `Delimiter` - Character for separating values (default: ',')
- `LengthMarker` - Optional '#' prefix for array lengths

**DecodeOptions:**
- `Indent` - Spaces per indentation level (default: 2)
- `Strict` - Enforce validation of lengths and structure (default: true)

## License

MIT License - see LICENSE file for details.

## Links

- [TOON Specification](https://github.com/toon-format/spec)
- [TypeScript Implementation](https://github.com/toon-format/toon)