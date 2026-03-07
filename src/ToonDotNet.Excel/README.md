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
- **Encode a single named sheet** from a workbook by name — produces a root array
- **Encode a subset of sheets** from a workbook by name list — produces a root object with only those keys
- **Encode directly from a file path** — open an `.xlsx` and get back a TOON string in one call
- **Save as `.toon`** — read an Excel file and write the result straight to a `.toon` file

### Decoding — TOON → Excel
- **Decode a TOON string** into a new `XLWorkbook` — root array becomes `Sheet1`, root object becomes one sheet per key
- **Load a `.toon` file** directly into a new `XLWorkbook`
- **Save as `.xlsx`** — decode a TOON string and write the result straight to an Excel file
- **Convert `.toon` to `.xlsx`** — file-to-file conversion in one call

### Async support
- Every encode and decode operation has an `*Async` counterpart
- In-memory operations (`EncodeAsync`, `DecodeAsync`) return already-completed tasks and honour cancellation before starting
- File-based operations (`EncodeFileAsync`, `SaveAsToonAsync`, `SaveAsExcelAsync`, `ConvertToonToExcelAsync`) offload I/O to avoid blocking the caller
- All async methods accept an optional `CancellationToken`

### Extension methods
- `IXLWorksheet.ToToon()` — encode a worksheet inline
- `IXLWorkbook.ToToon()` — encode all sheets inline
- `string.ToExcelWorkbook()` — decode a TOON string inline
- `IXLWorksheet.ToToonAsync()` — async worksheet encode inline
- `IXLWorkbook.ToToonAsync()` — async workbook encode inline
- `string.ToExcelWorkbookAsync()` — async TOON decode inline

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
- 101 unit tests, 100% passing, 84% code coverage

---

## Installation

```
dotnet add package Toon.DotNet.Excel
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
| Named sheet from workbook | Root array (same as encoding the worksheet directly) |
| Subset of sheets from workbook | Root object — only the selected sheet keys |

Decoding reverses the mapping: a root TOON object creates one sheet per key; a root array creates a single sheet named `Sheet1`.

---

## Quick start

### Encode a worksheet to TOON

```csharp
using ClosedXML.Excel;
using ToonFormat.Excel;

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

### Encode a single named sheet from a workbook

```csharp
// Encode only "Sales" — result is a root array, identical to encoding the worksheet directly
string toon = ToonExcel.Encode(workbook, "Sales");
```

### Encode a subset of sheets from a workbook

```csharp
// Encode only two of the four sheets — result is a root object with those two keys
string toon = ToonExcel.Encode(workbook, new[] { "Sales", "Costs" });
// Sales[3]{id,product,amount}:
//   ...
// Costs[2]{id,amount}:
//   ...
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

### Async encode and decode

```csharp
// Async worksheet encode
string toon = await workbook.Worksheet("Sales").ToToonAsync();

// Async full workbook encode
string toon = await workbook.ToToonAsync();

// Async file encode (offloads ClosedXML I/O to a background thread)
string toon = await ToonExcel.EncodeFileAsync("report.xlsx");

// Async file save
await ToonExcel.SaveAsToonAsync("report.xlsx", "report.toon");

// Async decode (string → workbook)
using var wb = await toon.ToExcelWorkbookAsync();

// Async save as Excel
await ToonExcel.SaveAsExcelAsync(toon, "output.xlsx");

// Async file-to-file conversion
await ToonExcel.ConvertToonToExcelAsync("report.toon", "output.xlsx");

// With cancellation
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
string toon = await ToonExcel.EncodeFileAsync("report.xlsx", cancellationToken: cts.Token);
```

---

## API overview

### `ToonExcel` static class

#### Encoding — Excel → TOON

| Method | Returns | Description |
|---|---|---|
| `ToonExcel.Encode(IXLWorksheet, EncodeOptions?)` | `string` | Encodes a single worksheet to a TOON root array |
| `ToonExcel.Encode(IXLWorkbook, EncodeOptions?)` | `string` | Encodes all sheets to a TOON root object, one key per sheet |
| `ToonExcel.Encode(IXLWorkbook, string, EncodeOptions?)` | `string` | Encodes a single named sheet — returns a root array |
| `ToonExcel.Encode(IXLWorkbook, IEnumerable<string>, EncodeOptions?)` | `string` | Encodes a subset of named sheets — returns a root object |
| `ToonExcel.EncodeFile(string, EncodeOptions?)` | `string` | Opens an `.xlsx` file and returns its TOON representation |
| `ToonExcel.SaveAsToon(string, string, EncodeOptions?)` | `void` | Converts an `.xlsx` file and saves the result as a `.toon` file |

#### Decoding — TOON → Excel

| Method | Returns | Description |
|---|---|---|
| `ToonExcel.Decode(string, DecodeOptions?)` | `XLWorkbook` | Decodes a TOON string into a new `XLWorkbook` |
| `ToonExcel.LoadToonFile(string, DecodeOptions?)` | `XLWorkbook` | Reads a `.toon` file and returns a new `XLWorkbook` |
| `ToonExcel.SaveAsExcel(string, string, DecodeOptions?)` | `void` | Decodes a TOON string and saves it as an `.xlsx` file |
| `ToonExcel.ConvertToonToExcel(string, string, DecodeOptions?)` | `void` | Converts a `.toon` file to an `.xlsx` file |

> **Note:** `Decode` and `LoadToonFile` return a new `XLWorkbook`. The caller is responsible for disposing it.

#### Async encoding — Excel → TOON

| Method | Returns | Description |
|---|---|---|
| `ToonExcel.EncodeAsync(IXLWorksheet, EncodeOptions?, CancellationToken)` | `Task<string>` | Async worksheet encode; completes synchronously (ClosedXML has no async API) |
| `ToonExcel.EncodeAsync(IXLWorkbook, EncodeOptions?, CancellationToken)` | `Task<string>` | Async workbook encode; completes synchronously |
| `ToonExcel.EncodeFileAsync(string, EncodeOptions?, CancellationToken)` | `Task<string>` | Opens an `.xlsx` file on a background thread and returns its TOON representation |
| `ToonExcel.SaveAsToonAsync(string, string, EncodeOptions?, CancellationToken)` | `Task` | Async file-to-file Excel → TOON conversion |

#### Async decoding — TOON → Excel

| Method | Returns | Description |
|---|---|---|
| `ToonExcel.DecodeAsync(string, DecodeOptions?, CancellationToken)` | `Task<XLWorkbook>` | Async TOON string decode; completes synchronously |
| `ToonExcel.SaveAsExcelAsync(string, string, DecodeOptions?, CancellationToken)` | `Task` | Decodes a TOON string and saves as `.xlsx` on a background thread |
| `ToonExcel.ConvertToonToExcelAsync(string, string, DecodeOptions?, CancellationToken)` | `Task` | Async file-to-file TOON → Excel conversion |

---

### Extension methods

#### Synchronous

```csharp
// IXLWorksheet → TOON root array
string toon = worksheet.ToToon(options);

// IXLWorkbook → TOON root object
string toon = workbook.ToToon(options);

// TOON string → XLWorkbook
using XLWorkbook wb = toonString.ToExcelWorkbook(options);
```

#### Asynchronous

```csharp
// IXLWorksheet → TOON root array (async)
string toon = await worksheet.ToToonAsync(options, cancellationToken);

// IXLWorkbook → TOON root object (async)
string toon = await workbook.ToToonAsync(options, cancellationToken);

// TOON string → XLWorkbook (async)
using XLWorkbook wb = await toonString.ToExcelWorkbookAsync(options, cancellationToken);
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

## Async behaviour notes

| Method group | Strategy |
|---|---|
| `EncodeAsync(worksheet/workbook)`, `DecodeAsync(string)` | Returns an already-completed `Task` — ClosedXML has no async API; cancellation is checked before the call starts |
| `EncodeFileAsync`, `SaveAsExcelAsync` | Uses `Task.Run` to offload synchronous ClosedXML file I/O to the thread pool |
| `SaveAsToonAsync` | Combines `EncodeFileAsync` (thread pool) with `File.WriteAllTextAsync` |
| `ConvertToonToExcelAsync` | Uses `File.ReadAllTextAsync` for the TOON read, then `Task.Run` for decode and Excel save |

---

## Dependencies

- [Toon.DotNet](https://www.nuget.org/packages/Toon.DotNet) — core TOON encoding and decoding
- [ClosedXML](https://github.com/ClosedXML/ClosedXML) — Excel file reading and writing

---

## License

[MIT](https://github.com/CharlesHunt/ToonDotNet/blob/master/LICENSE)



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
- 101 unit tests, 100% passing, 84% code coverage

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
