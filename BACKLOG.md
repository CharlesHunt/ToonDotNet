# ToonFormat — Feature Backlog

Useful functions and features not yet implemented, grouped by area.

---

## Core API

### Non-throwing parse (`TryDecode`)

| Signature | Returns |
|-----------|---------|
| `Toon.TryDecode(string, out JsonElement, DecodeOptions?)` | `bool` |
| `Toon.TryDecode<T>(string, out T?, DecodeOptions?, JsonSerializerOptions?)` | `bool` |
| `Toon.TryDecode(Stream, out JsonElement, DecodeOptions?, Encoding?)` | `bool` |

Follows the standard .NET `Try*` pattern. Returns `false` instead of throwing when the input is malformed, making it safe to use in hot paths without a `try/catch`.

---

### Stream and TextWriter / TextReader overloads

| Signature | Notes |
|-----------|-------|
| `Toon.Encode(object?, TextWriter, EncodeOptions?)` | Write to any `TextWriter` (e.g. `StringWriter`, `HttpResponse.Body`) |
| `Toon.Decode(TextReader, DecodeOptions?)` ? `JsonElement` | Read from any `TextReader` |
| `Toon.Decode<T>(TextReader, DecodeOptions?, JsonSerializerOptions?)` ? `T` | Typed read from `TextReader` |
| `Toon.Encode(DataTable, Stream, EncodeOptions?, Encoding?)` | `DataTable` encode direct to stream |

---

### Validation on streams

| Signature | Returns |
|-----------|---------|
| `Toon.IsValid(Stream, DecodeOptions?, Encoding?)` | `bool` |
| `Toon.IsValidAsync(Stream, DecodeOptions?, Encoding?, CancellationToken)` | `Task<bool>` |

Currently `IsValid` only accepts a `string`. These overloads avoid the caller having to read the stream to a string first.

---

### DataTable async overload

```csharp
Task Toon.SaveAsync(DataTable, string filePath, EncodeOptions?, CancellationToken)
```

The synchronous `Toon.Encode(DataTable)` exists, but there is no async file-save counterpart for `DataTable`.

---

## `EncodeOptions` Enhancements

| Property | Type | Description |
|----------|------|-------------|
| `NullHandling` | `NullHandling` enum (`Include` / `Omit`) | Whether null-valued fields are written to the output or silently skipped. Default: `Include`. |
| `DateTimeFormat` | `string?` | Custom format string applied to `DateTime` / `DateTimeOffset` values before encoding. Defaults to ISO-8601. |
| `MaxDepth` | `int` | Guards against deeply-nested object graphs by throwing when the limit is exceeded. |
| `LineEnding` | `LineEnding` enum (`LF` / `CRLF`) | Controls whether the output uses `\n` or `\r\n`. Defaults to `\n`. |

---

## `DecodeOptions` Enhancements

| Property | Type | Description |
|----------|------|-------------|
| `MaxDepth` | `int` | Guards against deeply-nested or adversarial input. |
| `AllowComments` | `bool` | Treat lines starting with `#` as comments and ignore them during parsing. |
| `AllowTrailingDelimiters` | `bool` | Tolerate a trailing delimiter at the end of a tabular row (common in hand-edited files). |

---

## Diagnostic / Utility

### `Toon.GetInfo`

```csharp
ToonDocumentInfo Toon.GetInfo(string toonString, DecodeOptions? options = null)
```

Returns structural metadata — root value kind, top-level key names, array lengths and field lists — without producing a full `JsonElement`. Useful for quick inspection or tooling.

```csharp
public class ToonDocumentInfo
{
    public JsonValueKind RootKind { get; }
    public IReadOnlyList<string> TopLevelKeys { get; }
    public IReadOnlyDictionary<string, int> ArrayLengths { get; }
    public IReadOnlyDictionary<string, string[]> TabularHeaders { get; }
}
```

---

### `Toon.SizeComparison` (structured result)

```csharp
ToonSizeComparison Toon.SizeComparison<T>(T input, EncodeOptions? options = null)
```

Complements the existing `SizeComparisonPercentage`. Returns raw byte counts alongside the reduction percentage.

```csharp
public class ToonSizeComparison
{
    public int JsonLength { get; }
    public int ToonLength { get; }
    public decimal ReductionPercent { get; }
}
```

---

### `Toon.Diff`

```csharp
ToonDiff Toon.Diff(object? before, object? after, EncodeOptions? options = null)
```

Field-level structural comparison. Highlights added, removed, and changed properties to support change-tracking scenarios and test assertions.

---

## Collection / Streaming (large dataset support)

| Signature | Notes |
|-----------|-------|
| `Toon.Encode<T>(IEnumerable<T>, EncodeOptions?)` ? `string` | Strongly-typed collection overload; avoids boxing through `object?` |
| `Toon.EncodeLines<T>(IEnumerable<T>, Stream, EncodeOptions?, Encoding?)` | Writes rows incrementally — low memory footprint for large collections |
| `Toon.EncodeAsync<T>(IAsyncEnumerable<T>, Stream, EncodeOptions?, Encoding?, CancellationToken)` | Async streaming encode; consumes `IAsyncEnumerable` directly (e.g. EF Core query results) |

---

## Format Conversion

### CSV ? Done

> **? Implemented** in [`Toon.DotNet.Csv`](src/Toon.DotNet.CSV) v1.7.0. All operations are available on the `ToonCsv` static class with additional stream overloads, `SaveAsToon` / `SaveAsToonAsync`, `ConvertToonToCsv`, and `CsvToToon` / `ToonToCsv` extension methods. See [`src/Toon.DotNet.CSV/README.md`](src/Toon.DotNet.CSV/README.md) for the full API.

| Signature | Notes |
|-----------|-------|
| ~~`Toon.FromCsv(string csv, EncodeOptions?)` ? `string`~~ | `ToonCsv.FromCsv(string, EncodeOptions?)` |
| ~~`Toon.FromCsvFile(string csvPath, EncodeOptions?)` ? `string`~~ | `ToonCsv.FromCsvFile(string, EncodeOptions?)` |
| ~~`Toon.FromCsvAsync(string csvPath, EncodeOptions?, CancellationToken)` ? `Task<string>`~~ | `ToonCsv.FromCsvAsync(string, EncodeOptions?, CancellationToken)` |
| ~~`Toon.ToCsv(string toon, DecodeOptions?)` ? `string`~~ | `ToonCsv.ToCsv(string, DecodeOptions?)` |
| ~~`Toon.ToCsvFile(string toon, string csvPath, DecodeOptions?)`~~ | `ToonCsv.ToCsvFile(string, string, DecodeOptions?)` |
| ~~`Toon.ToCsvAsync(string toon, string csvPath, DecodeOptions?, CancellationToken)` ? `Task`~~ | `ToonCsv.ToCsvAsync(string, string, DecodeOptions?, CancellationToken)` |

---

### Markdown

```csharp
string Toon.ToMarkdownTable(string toonString, DecodeOptions? options = null)
```

Converts a TOON tabular array to a GitHub-flavoured Markdown table. Useful for generating documentation or LLM prompt context that requires human-readable tables.

---

## Excel Integration (`Toon.DotNet.Excel`) Enhancements

### Async overloads

| Signature |
|-----------|
| `ToonExcel.EncodeAsync(IXLWorksheet, EncodeOptions?, CancellationToken)` ? `Task<string>` |
| `ToonExcel.EncodeAsync(IXLWorkbook, EncodeOptions?, CancellationToken)` ? `Task<string>` |
| `ToonExcel.EncodeFileAsync(string excelPath, EncodeOptions?, CancellationToken)` ? `Task<string>` |
| `ToonExcel.SaveAsToonAsync(string excelPath, string toonPath, EncodeOptions?, CancellationToken)` ? `Task` |
| `ToonExcel.DecodeAsync(string toonString, DecodeOptions?, CancellationToken)` ? `Task<XLWorkbook>` |
| `ToonExcel.SaveAsExcelAsync(string toonString, string excelPath, DecodeOptions?, CancellationToken)` ? `Task` |
| `ToonExcel.ConvertToonToExcelAsync(string toonPath, string excelPath, DecodeOptions?, CancellationToken)` ? `Task` |

### Async extension methods

| Signature |
|-----------|
| `IXLWorksheet.ToToonAsync(EncodeOptions?, CancellationToken)` ? `Task<string>` |
| `IXLWorkbook.ToToonAsync(EncodeOptions?, CancellationToken)` ? `Task<string>` |
| `string.ToExcelWorkbookAsync(DecodeOptions?, CancellationToken)` ? `Task<XLWorkbook>` |

### Sheet selection

| Signature | Notes |
|-----------|-------|
| `ToonExcel.Encode(IXLWorkbook, string sheetName, EncodeOptions?)` ? `string` | Encode a single named sheet from a workbook |
| `ToonExcel.Encode(IXLWorkbook, IEnumerable<string> sheetNames, EncodeOptions?)` ? `string` | Encode a subset of sheets |

---

## New Integration Packages (future NuGet packages)

| Package | Description |
|---------|-------------|
| ~~`Toon.DotNet.Csv`~~ | ? **Done** — released as [`Toon.DotNet.CSV`](src/Toon.DotNet.CSV) v1.7.0 with full streaming, async, and RFC 4180-compliant parsing via CsvHelper |
| `Toon.DotNet.AspNetCore` | ASP.NET Core output formatter so controllers can return TOON responses via `Accept: application/toon` |
| `Toon.DotNet.EFCore` | Encode `IQueryable<T>` / `DbSet<T>` results directly, streaming rows to avoid loading the full result set into memory |
| `Toon.DotNet.Dapper` | Encode `IEnumerable<dynamic>` Dapper query results, preserving column order from the reader |
