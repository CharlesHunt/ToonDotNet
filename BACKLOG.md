# ToonFormat � Feature Backlog

Useful functions and features not yet implemented, grouped by area.

---

## Core API

### Toon V4

This repository already has a mature core serializer and integration packages, so Toon V4 work should be scoped as a compatibility and feature-completion effort rather than a completely new implementation. The current focus is to validate and harden the existing parser/encoder behavior in the core library while preserving the public API and the multi-target support already defined in the solution.

#### Scope
- Core implementation: [src/Toon.DotNet/Toon.cs](src/Toon.DotNet/Toon.cs), [src/Toon.DotNet/ToonAsync.cs](src/Toon.DotNet/ToonAsync.cs), [src/Toon.DotNet/ToonStream.cs](src/Toon.DotNet/ToonStream.cs), and the parser/encoder folders under [src/Toon.DotNet/Decode](src/Toon.DotNet/Decode) and [src/Toon.DotNet/Encode](src/Toon.DotNet/Encode).
- Shared types and helpers: [src/Toon.DotNet/Types.cs](src/Toon.DotNet/Types.cs), [src/Toon.DotNet/Constants.cs](src/Toon.DotNet/Constants.cs), and the shared helpers in [src/Toon.DotNet/Shared](src/Toon.DotNet/Shared).
- Integration coverage: [src/Toon.DotNet.CSV/ToonCsv.cs](src/Toon.DotNet.CSV/ToonCsv.cs) and [src/Toon.DotNet.Excel/ToonExcel.cs](src/Toon.DotNet.Excel/ToonExcel.cs), which must continue to interoperate with the core serializer.
- Test surface: [tests/Toon.DotNet.Tests](tests/Toon.DotNet.Tests) with the existing xUnit suites such as [tests/Toon.DotNet.Tests/ToonParserTests.cs](tests/Toon.DotNet.Tests/ToonParserTests.cs), [tests/Toon.DotNet.Tests/ToonEncoderTests.cs](tests/Toon.DotNet.Tests/ToonEncoderTests.cs), [tests/Toon.DotNet.Tests/ToonDecoderTests.cs](tests/Toon.DotNet.Tests/ToonDecoderTests.cs), [tests/Toon.DotNet.Tests/StreamTests.cs](tests/Toon.DotNet.Tests/StreamTests.cs), [tests/Toon.DotNet.Tests/ToonCsvTests.cs](tests/Toon.DotNet.Tests/ToonCsvTests.cs), and [tests/Toon.DotNet.Tests/ToonExcelTests.cs](tests/Toon.DotNet.Tests/ToonExcelTests.cs).

#### Compatibility strategy
- Preserve the current public entry points in [src/Toon.DotNet/Toon.cs](src/Toon.DotNet/Toon.cs) and avoid breaking changes unless a clear v4 requirement demands it.
- Maintain compatibility with the existing target frameworks declared in [src/Toon.DotNet/Toon.DotNet.csproj](src/Toon.DotNet/Toon.DotNet.csproj), [src/Toon.DotNet.CSV/Toon.DotNet.CSV.csproj](src/Toon.DotNet.CSV/Toon.DotNet.CSV.csproj), and [src/Toon.DotNet.Excel/Toon.DotNet.Excel.csproj](src/Toon.DotNet.Excel/Toon.DotNet.Excel.csproj).
- Continue to leverage the current `#if !NETSTANDARD2_0` guards where needed for newer APIs and keep the .NET Standard 2.0 story intact.
- Prefer additive changes to `EncodeOptions` and `DecodeOptions` over signature churn so downstream consumers are not forced to rewrite their code.

#### Testing plan
- Expand the test suite in [tests/Toon.DotNet.Tests](tests/Toon.DotNet.Tests) with dedicated v4 parser, encoder, and round-trip tests instead of relying on a single smoke test.
- Cover the existing high-value paths already exercised by the current suites: basic serialization, delimiter handling, stream and text-reader/writer operations, file I/O, CSV conversion, and Excel conversion.
- Keep the regression baseline aligned with the repo’s current packaging and multi-targeting expectations.

#### Documentation and release planning
- Update [README.md](README.md), [CHANGELOG.md](CHANGELOG.md), and the package-specific docs in [src/Toon.DotNet.CSV/README.md](src/Toon.DotNet.CSV/README.md) and [src/Toon.DotNet.Excel/README.md](src/Toon.DotNet.Excel/README.md) as the implementation becomes stable.
- Deliver the work in two phases: core parser/encoder and regression tests first, then docs/package release notes once validation is complete.
- Treat the rollout as a feature release unless compatibility review proves that a breaking change is necessary.

#### Acceptance criteria
- Toon v4 examples encode and decode correctly through the core library without introducing regressions in existing behavior.
- Existing public APIs remain callable for the current target frameworks.
- CSV and Excel integration packages continue to round-trip data with the same expected behavior.
- The test suite passes with new v4-focused cases added and the repo documentation clearly states the implementation status.
- Release notes explicitly describe the supported v4 scope and any compatibility caveats for downstream users.

### Non-throwing parse (`TryDecode`)

| Signature | Returns |
|-----------|---------|
| `Toon.TryDecode(string, out JsonElement, DecodeOptions?)` | `bool` |
| `Toon.TryDecode<T>(string, out T?, DecodeOptions?, JsonSerializerOptions?)` | `bool` |
| `Toon.TryDecode(Stream, out JsonElement, DecodeOptions?, Encoding?)` | `bool` |

Follows the standard .NET `Try*` pattern. Returns `false` instead of throwing when the input is malformed, making it safe to use in hot paths without a `try/catch`.

---

### Non-throwing encode (`TryEncode`)

| Signature | Returns |
|-----------|---------|
| `Toon.TryEncode(object?, out string?, EncodeOptions?)` | `bool` |
| `Toon.TryEncode(object?, out string?, out Exception?, EncodeOptions?)` | `bool` |
| `Toon.TryEncode(DataTable, out string?, EncodeOptions?)` | `bool` |

Follows the standard .NET `Try*` pattern. Returns `false` and sets the `out string?` to `null` instead of throwing when encoding fails (e.g. circular references, unsupported types, or serialisation errors), making it safe to use in hot paths without a `try/catch`. The overload that includes `out Exception?` captures the underlying exception for logging or diagnostics while still avoiding a thrown exception at the call site.

---

### Stream and TextWriter / TextReader overloads � *Implemented*

| Signature | Notes |
|-----------|-------|
| ~~`Toon.Encode(object?, TextWriter, EncodeOptions?)`~~ | `Toon.Encode(object?, TextWriter, EncodeOptions?)` |
| ~~`Toon.Decode(TextReader, DecodeOptions?)` ? `JsonElement`~~ | `Toon.Decode(TextReader, DecodeOptions?)` |
| ~~`Toon.Decode<T>(TextReader, DecodeOptions?, JsonSerializerOptions?)` ? `T`~~ | `Toon.Decode<T>(TextReader, DecodeOptions?, JsonSerializerOptions?)` |
| ~~`Toon.Encode(DataTable, Stream, EncodeOptions?, Encoding?)`~~ | `Toon.Encode(DataTable, Stream, EncodeOptions?, Encoding?)` � `.NET 8+` only |

Implemented in `ToonStream.cs`. `TextWriter` / `TextReader` overloads delegate to the string `Encode` / `Decode` paths and are available on all targets. `Encode(DataTable, Stream, ...)` is guarded by `#if !NETSTANDARD2_0`. 24 new tests added in `TextReaderWriterTests.cs`.

---

### Validation on streams

| Signature | Returns |
|-----------|---------|
| `Toon.IsValid(Stream, DecodeOptions?, Encoding?)` | `bool` |
| `Toon.IsValidAsync(Stream, DecodeOptions?, Encoding?, CancellationToken)` | `Task<bool>` |

Currently `IsValid` only accepts a `string`. These overloads avoid the caller having to read the stream to a string first.

---

### DataTable async overload ? *Implemented*

```csharp
Task Toon.SaveAsync(DataTable, string filePath, EncodeOptions?, CancellationToken)
```

~~The synchronous `Toon.Encode(DataTable)` exists, but there is no async file-save counterpart for `DataTable`.~~

Implemented in `ToonAsync.cs`. Guarded by `#if !NETSTANDARD2_0`. Validates `table` (ArgumentNullException) and `filePath` (ArgumentException). 16 new tests added in `DataTableSaveAsyncTests.cs`.

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

Returns structural metadata � root value kind, top-level key names, array lengths and field lists � without producing a full `JsonElement`. Useful for quick inspection or tooling.

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
| `Toon.EncodeLines<T>(IEnumerable<T>, Stream, EncodeOptions?, Encoding?)` | Writes rows incrementally � low memory footprint for large collections |
| `Toon.EncodeAsync<T>(IAsyncEnumerable<T>, Stream, EncodeOptions?, Encoding?, CancellationToken)` | Async streaming encode; consumes `IAsyncEnumerable` directly (e.g. EF Core query results) |

---

## Format Conversion

### CSV ? *Implemented*

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

### Async overloads � *Implemented*

| Signature |
|-----------|
| ~~`ToonExcel.EncodeAsync(IXLWorksheet, EncodeOptions?, CancellationToken)` ? `Task<string>`~~ |
| ~~`ToonExcel.EncodeAsync(IXLWorkbook, EncodeOptions?, CancellationToken)` ? `Task<string>`~~ |
| ~~`ToonExcel.EncodeFileAsync(string excelPath, EncodeOptions?, CancellationToken)` ? `Task<string>`~~ |
| ~~`ToonExcel.SaveAsToonAsync(string excelPath, string toonPath, EncodeOptions?, CancellationToken)` ? `Task`~~ |
| ~~`ToonExcel.DecodeAsync(string toonString, DecodeOptions?, CancellationToken)` ? `Task<XLWorkbook>`~~ |
| ~~`ToonExcel.SaveAsExcelAsync(string toonString, string excelPath, DecodeOptions?, CancellationToken)` ? `Task`~~ |
| ~~`ToonExcel.ConvertToonToExcelAsync(string toonPath, string excelPath, DecodeOptions?, CancellationToken)` ? `Task`~~ |

In-memory overloads (`EncodeAsync(worksheet/workbook)` and `DecodeAsync(string)`) complete synchronously via `Task.FromResult` since ClosedXML has no async API. File-based overloads (`EncodeFileAsync`, `SaveAsExcelAsync`) use `Task.Run` to offload synchronous ClosedXML I/O; `SaveAsToonAsync` and `ConvertToonToExcelAsync` combine async file reads with `Task.Run` for workbook work. Implemented in `ToonExcel.cs`.

### Async extension methods � *Implemented*

| Signature |
|-----------|
| ~~`IXLWorksheet.ToToonAsync(EncodeOptions?, CancellationToken)` ? `Task<string>`~~ |
| ~~`IXLWorkbook.ToToonAsync(EncodeOptions?, CancellationToken)` ? `Task<string>`~~ |
| ~~`string.ToExcelWorkbookAsync(DecodeOptions?, CancellationToken)` ? `Task<XLWorkbook>`~~ |

Implemented in `ExcelToonExtensions.cs`. Each method delegates to the corresponding `ToonExcel` async method.

### Sheet selection � *Implemented*

| Signature | Notes |
|-----------|-------|
| ~~`ToonExcel.Encode(IXLWorkbook, string sheetName, EncodeOptions?)` ? `string`~~ | Encodes a single named sheet; result is a root TOON array |
| ~~`ToonExcel.Encode(IXLWorkbook, IEnumerable<string> sheetNames, EncodeOptions?)` ? `string`~~ | Encodes a subset of sheets; result is a root TOON object |

Implemented in `ToonExcel.cs`. `Encode(workbook, sheetName)` delegates to `Encode(IXLWorksheet)` and returns a root array. `Encode(workbook, sheetNames)` builds a dictionary of the requested sheets and returns a root object. Both throw `ArgumentException` when a requested sheet name is not found. 45 new tests added in `ToonExcelTests.cs`.

---

## New Integration Packages (future NuGet packages)

| Package | Description |
|---------|-------------|
| ~~`Toon.DotNet.Csv`~~ | ? **Done** � released as [`Toon.DotNet.CSV`](src/Toon.DotNet.CSV) v1.7.0 with full streaming, async, and RFC 4180-compliant parsing via CsvHelper |
| `Toon.DotNet.AspNetCore` | ASP.NET Core output formatter so controllers can return TOON responses via `Accept: application/toon` |
| `Toon.DotNet.EFCore` | Encode `IQueryable<T>` / `DbSet<T>` results directly, streaming rows to avoid loading the full result set into memory |
| `Toon.DotNet.Dapper` | Encode `IEnumerable<dynamic>` Dapper query results, preserving column order from the reader |
