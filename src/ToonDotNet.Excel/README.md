# Toon.DotNet.Excel
---

[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-blue.svg)![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blue.svg)![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download)
[![.NET](https://github.com/CharlesHunt/ToonDotNet/actions/workflows/dotnet.yml/badge.svg)](https://github.com/CharlesHunt/ToonDotNet/actions/workflows/dotnet.yml)
[![Nuget](https://img.shields.io/badge/platform-win%20|%20unix%20|%20osx-orange.svg)](https://dotnet.microsoft.com/download)

[![NuGet](https://img.shields.io/nuget/v/Toon.DotNet.Excel.svg)](https://www.nuget.org/packages/Toon.DotNet.Excel)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Toon.DotNet.Excel.svg?label=Downloads&color=green)](https://www.nuget.org/packages/Toon.DotNet.Excel)


[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)


---
### Excel integration for [ToonDotNet](https://www.nuget.org/packages/Toon.DotNet) — convert Excel workbooks and worksheets to and from [TOON format](https://github.com/CharlesHunt/ToonDotNet).

## Features

### Encoding — Excel → TOON
- **Encode a single worksheet** to a TOON root array, using the first row as column headers
- **Encode a full workbook** to a TOON root object, one key per sheet name
- **Encode directly from a file path** — open an `.xlsx` and get back a TOON string in one call
- **Save as `.toon`** — read an Excel file and write the result straight to a `.toon` file

### Decoding — TOON → Excel
- **Decode a TOON string** into a new `XLWorkbook` — root array becomes `Sheet1`, root object becomes one sheet per key
- **Load a `.toon` file** directly into a new `XLWorkbook`
- **Save as `.xlsx`** — decode a TOON string and write the result straight to an Excel file
- **Convert `.toon` to `.xlsx`** — file-to-file conversion in one call

### Extension methods
- `IXLWorksheet.ToToon()` — encode a worksheet inline
- `IXLWorkbook.ToToon()` — encode all sheets inline
- `string.ToExcelWorkbook()` — decode a TOON string inline

### Cell type handling
- Numbers, booleans, text and blank/null cells are round-tripped faithfully
- `DateTime` values are encoded as ISO 8601 strings and parsed back on decode
- `TimeSpan` values are preserved as formatted strings

### Sheet management
- Multi-sheet workbooks are fully supported
- Sheet names are automatically sanitised (forbidden Excel characters removed, truncated to 31 characters)
- Duplicate sheet names after sanitisation receive a numeric suffix, e.g. `Sales (2)`

### Other
- All encode/decode calls accept the standard `EncodeOptions` and `DecodeOptions` from `Toon.DotNet`
- 56 unit tests, 100% passing, 78% code coverage

---

## Installation

```
dotnet add package ToonDotNet.Excel
```

---

## Compatibility

- .NET 10.0
- .NET 9.0
- .NET 8.0

---

## How it works

Each worksheet is treated as tabular data: the **first row** provides column headers and every subsequent row becomes a TOON data row.

| Excel structure | TOON output |
|---|---|
| Single worksheet | Root array `[N]{col1,col2,...}:` |
| Full workbook | Root object — one key per sheet name |

Decoding reverses the mapping: a root TOON object creates one sheet per key; a root array creates a single sheet named `Sheet1`.

---

## Quick start

### Encode a worksheet to TOON

```csharp
using ClosedXML.Excel;
using ToonDotNet.Excel;

using var workbook = new XLWorkbook("report.xlsx");

// Single sheet → root array
string toon = workbook.Worksheet("Sales").ToToon();
// [3]{id,product,amount}:
//   1,Widget,9.99
//   2,Gadget,19.99
//   3,Doohickey,4.99
```

### Encode a full workbook to TOON

```csharp
// All sheets → root object keyed by sheet name
string toon = workbook.ToToon();
// Sales[3]{id,product,amount}:
//   1,Widget,9.99
//   ...
// Customers[2]{id,name}:
//   1,Alice
//   2,Bob
```

### Decode TOON back to an Excel workbook

```csharp
using var decoded = toon.ToExcelWorkbook();
decoded.SaveAs("output.xlsx");
```

### Convert an Excel file directly to a TOON file

```csharp
ToonExcel.SaveAsToon("report.xlsx", "report.toon");
```

### Convert a TOON file directly to an Excel file

```csharp
ToonExcel.ConvertToonToExcel("report.toon", "report.xlsx");
```

---

## API overview

### `ToonExcel` static class

#### Encoding (Excel → TOON)

| Method | Description |
|---|---|
| `ToonExcel.Encode(IXLWorksheet, EncodeOptions?)` | Encodes a single worksheet to a TOON string (root array) |
| `ToonExcel.Encode(IXLWorkbook, EncodeOptions?)` | Encodes all sheets to a TOON string (root object keyed by sheet name) |
| `ToonExcel.EncodeFile(string excelPath, EncodeOptions?)` | Opens an `.xlsx` file and returns its TOON representation |
| `ToonExcel.SaveAsToon(string excelPath, string toonPath, EncodeOptions?)` | Converts an `.xlsx` file and saves the result as a `.toon` file |

#### Decoding (TOON → Excel)

| Method | Description |
|---|---|
| `ToonExcel.Decode(string toon, DecodeOptions?)` | Decodes a TOON string into a new `XLWorkbook` |
| `ToonExcel.LoadToonFile(string toonPath, DecodeOptions?)` | Reads a `.toon` file and returns a new `XLWorkbook` |
| `ToonExcel.SaveAsExcel(string toon, string excelPath, DecodeOptions?)` | Decodes a TOON string and saves it as an `.xlsx` file |
| `ToonExcel.ConvertToonToExcel(string toonPath, string excelPath, DecodeOptions?)` | Converts a `.toon` file to an `.xlsx` file |

> **Note:** `Decode` and `LoadToonFile` return a new `XLWorkbook` instance. The caller is responsible for disposing it.

---

### Extension methods

```csharp
// IXLWorksheet
string toon = worksheet.ToToon(options);

// IXLWorkbook
string toon = workbook.ToToon(options);

// string
using XLWorkbook wb = toonString.ToExcelWorkbook(options);
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
string toon = workbook.ToToon(opts);
```

---

## Cell type handling

| Excel / TOON type | Encoding | Decoding |
|---|---|---|
| Number | `double` | `double` written to cell |
| Boolean | `true` / `false` | Boolean cell value |
| DateTime | ISO 8601 string | Parsed back to `DateTime` cell |
| TimeSpan | Formatted string | String cell value |
| Text | String literal | String cell value |
| Blank / null | `null` | Empty cell |

---

## Dependencies

- [Toon.DotNet](https://www.nuget.org/packages/Toon.DotNet) — core TOON encoding and decoding
- [ClosedXML](https://github.com/ClosedXML/ClosedXML) — Excel file reading and writing

---

## License

[MIT](https://github.com/CharlesHunt/ToonDotNet/blob/master/LICENSE)
