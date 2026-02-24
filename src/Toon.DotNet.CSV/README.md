# Toon.DotNet.CSV
---

[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![.NET](https://github.com/CharlesHunt/ToonDotNet/actions/workflows/dotnet.yml/badge.svg)](https://github.com/CharlesHunt/ToonDotNet/actions/workflows/dotnet.yml)

[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-blue.svg)![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blue.svg)![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download)

[![NuGet](https://img.shields.io/nuget/v/Toon.DotNet.CSV.svg)](https://www.nuget.org/packages/Toon.DotNet.CSV)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Toon.DotNet.CSV.svg?label=Downloads&color=green)](https://www.nuget.org/packages/Toon.DotNet.CSV)

---
### CSV integration for [ToonDotNet](https://www.nuget.org/packages/Toon.DotNet) — convert CSV files, strings, and streams to and from [TOON format](https://github.com/CharlesHunt/ToonDotNet).

## Features

### Encoding — CSV → TOON
- **Convert a CSV string** directly to a TOON tabular array
- **Convert a CSV stream** — read from any readable `Stream`
- **Open a CSV file** and return its TOON representation in one call
- **Save as `.toon`** — read a CSV file and write the TOON result straight to a file
- **Async file support** — `FromCsvAsync` and `SaveAsToonAsync` for non-blocking I/O

### Decoding — TOON → CSV
- **Convert a TOON string** (root array of objects) to a CSV string
- **Write to a stream** — output CSV bytes to any writable `Stream`
- **Save as `.csv`** — decode a TOON string and write the result straight to a file
- **Convert a `.toon` file** directly to a `.csv` file in one call
- **Async file support** — `ToCsvAsync` for non-blocking I/O

### Extension methods
- `string.CsvToToon()` — convert a CSV string to TOON inline
- `string.ToonToCsv()` — convert a TOON string to CSV inline

### Type coercion
- Integer, floating-point, and boolean values are parsed from CSV text to their native types
- Empty fields are treated as `null`
- All other values are preserved as strings
- Type fidelity is maintained through the full CSV → TOON → CSV round-trip

### Other
- Built on [CsvHelper](https://joshclose.github.io/CsvHelper/) for robust, RFC 4180-compliant CSV parsing and writing
- All encode/decode calls accept the standard `EncodeOptions` and `DecodeOptions` from `Toon.DotNet`
- Streams are left open after read and write calls
- 69 unit tests, 100% passing, 88% code coverage

---

## Installation

```
dotnet add package Toon.DotNet.CSV
```

---

## Compatibility

- .NET 10.0
- .NET 9.0
- .NET 8.0

---

## How it works

CSV data is treated as tabular — the **first row** provides column headers and every subsequent row becomes a TOON data row. The result is a root TOON array.

| CSV structure | TOON output |
|---|---|
| Header row + N data rows | Root array `[N]{col1,col2,...}:` |

Decoding reverses the mapping: the TOON root value must be an array of objects. Property names become CSV column headers and each object becomes a data row.

---

## Quick start

### Convert a CSV string to TOON

```csharp
using ToonFormat.Csv;

string csv = """
    id,name,role
    1,Alice,admin
    2,Bob,user
    """;

string toon = ToonCsv.FromCsv(csv);
// [2]{id,name,role}:
//   1,Alice,admin
//   2,Bob,user
```

### Convert a CSV file to TOON

```csharp
string toon = ToonCsv.FromCsvFile("users.csv");
```

### Convert a CSV file to TOON asynchronously

```csharp
string toon = await ToonCsv.FromCsvAsync("users.csv");
```

### Convert a TOON string to CSV

```csharp
string toon = "[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user";

string csv = ToonCsv.ToCsv(toon);
// id,name,role
// 1,Alice,admin
// 2,Bob,user
```

### Save a CSV file as a TOON file

```csharp
ToonCsv.SaveAsToon("users.csv", "users.toon");

// Async version
await ToonCsv.SaveAsToonAsync("users.csv", "users.toon");
```

### Convert a TOON file directly to a CSV file

```csharp
ToonCsv.ConvertToonToCsv("users.toon", "users.csv");
```

### Read from and write to streams

```csharp
// CSV stream → TOON string
using var csvStream = File.OpenRead("users.csv");
string toon = ToonCsv.FromCsv(csvStream);

// TOON string → CSV stream
using var outputStream = File.OpenWrite("users.csv");
ToonCsv.ToCsvStream(toon, outputStream);
```

### Use extension methods

```csharp
// CSV string → TOON
string toon = "id,name\n1,Alice\n2,Bob".CsvToToon();

// TOON → CSV string
string csv = "[2]{id,name}:\n  1,Alice\n  2,Bob".ToonToCsv();
```

---

## API overview

### `ToonCsv` static class

#### Encoding (CSV → TOON)

| Method | Description |
|---|---|
| `ToonCsv.FromCsv(string csv, EncodeOptions?)` | Converts a CSV string to a TOON tabular array |
| `ToonCsv.FromCsv(Stream csvStream, EncodeOptions?, Encoding?)` | Reads CSV from a stream and returns a TOON string |
| `ToonCsv.FromCsvFile(string csvPath, EncodeOptions?)` | Opens a CSV file and returns its TOON representation |
| `ToonCsv.SaveAsToon(string csvPath, string toonPath, EncodeOptions?)` | Converts a CSV file and saves the result as a `.toon` file |
| `ToonCsv.FromCsvAsync(string csvPath, EncodeOptions?, CancellationToken)` | Asynchronously opens a CSV file and returns its TOON representation |
| `ToonCsv.SaveAsToonAsync(string csvPath, string toonPath, EncodeOptions?, CancellationToken)` | Asynchronously converts a CSV file and saves the result as a `.toon` file |

#### Decoding (TOON → CSV)

| Method | Description |
|---|---|
| `ToonCsv.ToCsv(string toon, DecodeOptions?)` | Converts a TOON string to a CSV string |
| `ToonCsv.ToCsvStream(string toon, Stream outputStream, DecodeOptions?, Encoding?)` | Converts a TOON string to CSV and writes it to a stream |
| `ToonCsv.ToCsvFile(string toon, string csvPath, DecodeOptions?)` | Converts a TOON string to CSV and writes it to a file |
| `ToonCsv.ToCsvAsync(string toon, string csvPath, DecodeOptions?, CancellationToken)` | Asynchronously converts a TOON string to CSV and writes it to a file |
| `ToonCsv.ConvertToonToCsv(string toonPath, string csvPath, DecodeOptions?)` | Converts a `.toon` file to a `.csv` file |

---

### Extension methods

```csharp
// string (CSV) → TOON
string toon = csvString.CsvToToon(options);

// string (TOON) → CSV
string csv = toonString.ToonToCsv(options);
```

---

## Options

Options are passed directly from [ToonDotNet](https://www.nuget.org/packages/Toon.DotNet) and work identically here.

### `EncodeOptions`

| Property | Default | Description |
|---|---|---|
| `Indent` | `2` | Spaces per indentation level |
| `Delimiter` | `','` | Column delimiter for tabular rows |
| `LengthMarker` | `null` | Optional prefix for array lengths (e.g. `'#'` produces `[#3]`) |

### `DecodeOptions`

| Property | Default | Description |
|---|---|---|
| `Indent` | `2` | Expected spaces per indentation level |
| `Strict` | `true` | Validate array lengths and row counts |

```csharp
var opts = new EncodeOptions { Delimiter = '|', Indent = 4 };
string toon = ToonCsv.FromCsv(csv, opts);
```

---

## Type coercion

When reading CSV, field values are automatically coerced to their native types before encoding to TOON. This preserves type information that is otherwise lost in plain-text CSV.

| CSV value | Parsed as | TOON encoding |
|---|---|---|
| `42` | `long` | `42` |
| `3.14` | `double` | `3.14` |
| `true` / `false` | `bool` | `true` / `false` |
| `Alice` | `string` | `Alice` |
| *(empty)* | `null` | `null` |

When converting TOON back to CSV, numbers and booleans are written as their raw text representations and empty fields are written for `null` values.

---

## Dependencies

- [Toon.DotNet](https://www.nuget.org/packages/Toon.DotNet) — core TOON encoding and decoding
- [CsvHelper](https://joshclose.github.io/CsvHelper/) — RFC 4180-compliant CSV parsing and writing

---

## License

[MIT](https://github.com/CharlesHunt/ToonDotNet/blob/master/LICENSE)
