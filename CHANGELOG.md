# Changelog

All notable changes to this project will be documented in this file. The format is based on Keep a Changelog.

**Versioning:** as of v3.3.2, the **core `Toon.DotNet` package's version tracks the [TOON specification](https://github.com/toon-format/spec) version it implements**, rather than semantic versioning against its own prior release history — `3.3.2` means "conforms to TOON spec v3.3.2," not "the third major revision of this library's API." Compatibility-relevant changes are still called out explicitly under each release (see the "Breaking changes" notes below) since the version number itself no longer signals API stability the way semver does. The **`Toon.DotNet.CSV`** and **`Toon.DotNet.Excel`** integration packages are not implementations of the spec themselves and continue to follow ordinary semantic versioning on their own numbering track.

## [1.0.0] -2025-11-04
### Added
- Initial public release of ToonFormat for .NET9
- Encoding from arbitrary .NET objects to TOON
- Decoding to `JsonElement` and strongly-typed models
- Validation and round-trip helpers
- Examples project and basic README

## [1.1.0] -2025-11-14
### Added
- Bumpred project to framework NET10

## [1.2.0] -2025-11-14
### Added
- Made the project multi framework, with .NET8.0, .NET9.0 and NET10.0

## [1.3.0] -2025-11-20
### Added
-- Added SizeComparisonPercentage function to compare TOON and JSON sizes. If TOON is smaller, returns the size reduction percentage; e.g. a value of 75 means TOON is 25% smaller than JSON or that the TOON size is 75% of the JSON equivalent.

## [1.4.0] -2025-11-25
### Added
-- Added compatibility for NetStandard2.0 so that the library can be used in older projects that target .NET Framework 4.6.1 and above.

## [1.5.0] -2025-12-01
### Added
-- Added basic file operations to read TOON data from files and write TOON data to files.

## [1.5.1] -2025-12-02
### Added
-- Added Load operation that returns JsonElement.

## [1.5.2] -2026-01-05
### Changed
-- Now uses System.Text.Json version 10.0.1.

## [1.6.0] - 2026-01-06
### Added
- **FromJson** - Efficient JSON-to-TOON conversion method that parses JSON strings directly to TOON format
- **FromJsonFile** - Convert JSON files to TOON format efficiently
- **ToJson** - Efficient TOON-to-JSON conversion method that produces JSON strings from TOON format
- **ToJsonFile** - Convert TOON files to JSON format efficiently
- **SaveAsJson** - Save TOON strings as formatted JSON files
- Bidirectional conversion support enables seamless interoperability between TOON and JSON formats

### Performance
- FromJson uses a single parse operation (JSON → JsonElement → TOON) instead of the less efficient (JSON → Object → JSON → JsonElement → TOON) path
- ToJson leverages the existing Decode infrastructure for efficient TOON → JsonElement → JSON conversion
- Minimal memory allocations and faster execution compared to deserializing to objects first
- Full control over JSON output formatting (compact or indented)

### Benefits
- Perfect for LLM workflows: compact TOON for prompts, JSON for system integration
- Maintains data fidelity through round-trip conversions
- Compatible with all existing encode/decode options

## [1.6.1] - 2026-01-26
### Added
- Encode **DataTable** - Convert a data table to TOON format.

## [1.7.0] - 2026-02-01
### Added
- New package: **Toon.DotNet.CSV** v1.7.0 — CSV integration for ToonDotNet
  - `ToonCsv.FromCsv(string, EncodeOptions?)` — convert a CSV string to a TOON tabular array
  - `ToonCsv.FromCsv(Stream, EncodeOptions?, Encoding?)` — read CSV from any readable stream
  - `ToonCsv.FromCsvFile(string, EncodeOptions?)` — open a CSV file and return its TOON representation
  - `ToonCsv.SaveAsToon(string, string, EncodeOptions?)` — convert a CSV file and save the result as a `.toon` file
  - `ToonCsv.FromCsvAsync(string, EncodeOptions?, CancellationToken)` — async CSV file to TOON string
  - `ToonCsv.SaveAsToonAsync(string, string, EncodeOptions?, CancellationToken)` — async CSV file to TOON file
  - `ToonCsv.ToCsv(string, DecodeOptions?)` — convert a TOON tabular array to a CSV string
  - `ToonCsv.ToCsvStream(string, Stream, DecodeOptions?, Encoding?)` — write CSV to any writable stream
  - `ToonCsv.ToCsvFile(string, string, DecodeOptions?)` — convert a TOON string and save as a `.csv` file
  - `ToonCsv.ToCsvAsync(string, string, DecodeOptions?, CancellationToken)` — async TOON string to CSV file
  - `ToonCsv.ConvertToonToCsv(string, string, DecodeOptions?)` — file-to-file `.toon` → `.csv` conversion
  - `string.CsvToToon()` / `string.ToonToCsv()` — inline extension methods
  - Automatic type coercion: integer, floating-point, and boolean CSV values are parsed to their native types
  - Built on [CsvHelper](https://joshclose.github.io/CsvHelper/) for RFC 4180-compliant parsing and writing

## [1.7.1] - 2026-02-08
### Added
- New package: **Toon.DotNet.Excel** v1.7.1 — Excel integration for ToonDotNet
  - `ToonExcel.Encode(IXLWorksheet, EncodeOptions?)` — encode a single worksheet to a TOON root array (first row used as column headers)
  - `ToonExcel.Encode(IXLWorkbook, EncodeOptions?)` — encode all worksheets to TOON root object, one key per sheet name
  - `ToonExcel.EncodeFile(string, EncodeOptions?)` — open an `.xlsx` file and return its TOON representation
  - `ToonExcel.SaveAsToon(string, string, EncodeOptions?)` — convert an Excel file and save the result as a `.toon` file
  - `ToonExcel.Decode(string, DecodeOptions?)` — decode a TOON string into a new `XLWorkbook`
  - `ToonExcel.LoadToonFile(string, DecodeOptions?)` — read a `.toon` file directly into a new `XLWorkbook`
  - `ToonExcel.SaveAsExcel(string, string, DecodeOptions?)` — decode a TOON string and write the result as an `.xlsx` file
  - `ToonExcel.ConvertToonToExcel(string, string, DecodeOptions?)` — file-to-file `.toon` → `.xlsx` conversion
  - `IXLWorksheet.ToToon()` / `IXLWorkbook.ToToon()` / `string.ToExcelWorkbook()` — inline extension methods
  - Numbers, booleans, text, `DateTime` (ISO 8601), `TimeSpan`, and blank/null cells are all handled and round-tripped faithfully
  - Multi-sheet workbooks fully supported; sheet names are automatically sanitised and de-duplicated
  - Built on [ClosedXML](https://github.com/ClosedXML/ClosedXML)
- **Toon.DotNet.CSV** bumped to v1.7.1 to align with the core library and Excel package release
- Core library (**Toon.DotNet**) — `Toon.SaveAsync(DataTable, string, EncodeOptions?, CancellationToken)` — async file-save overload for `DataTable`; guarded by `#if !NETSTANDARD2_0`

## [3.3.2] - 2026-09-11
A spec-compliance release, and the first under the new spec-aligned versioning scheme (see the note at the top of this file) — jumping from `1.7.4` to `3.3.2` reflects that scheme change, not 1.6 major versions' worth of prior work. A full audit of the core parser/encoder against the TOON spec v3.0.x–v3.3.2 rule set ([TOON_V3.md](TOON_V3.md)) found 21 gaps — 12 correctness-risk (wrong or lost data, or a crash on valid input), 6 interop-risk, 3 minor. **All 21 are fixed in this release**, bringing the core library into full conformance with TOON spec v3.3.2 — hence the version number. Two related round-trip fidelity gaps were also found and fixed in the CSV integration package (bumped independently to `1.8.0` under its own semver track) while verifying these fixes propagated correctly downstream. TOON v4.0+ features (comments, keyed-tabular objects, nested field groups, and more) remain unimplemented, tracked separately in [TOON_V4.md](TOON_V4.md) — the version will move to the v4.x line once that work lands.

### Breaking changes
The version number itself no longer flags these the way a semver major bump would (see the versioning note at the top of this file), so read this section carefully rather than relying on the version jump alone. Several of these are behavior changes under the **existing default options** — no new flag needs to be opted into for them to take effect:
- **Duplicate sibling object keys now throw in strict mode** (the default). Previously silently resolved via last-write-wins regardless of `Strict`.
- **Empty string input is now valid.** `Toon.Decode("")`, `Toon.IsValid("")`, `Toon.ToJson("")`, `Toon.SaveAsJson("")`, and loading an empty file via `Toon.Load`/`Toon.Load<T>` previously threw; they now succeed, decoding to `{}` (or a default-valued instance for `Load<T>`). Only `null` input is rejected now.
- **Leading-zero numeric tokens** (`"05"`, `"0001"`, `"-05"`) **now decode as strings, not numbers.** Affects typed deserialization if such values were previously (mis)coerced into numeric properties.
- **Empty tokens between delimiters now decode to `""` instead of `null`.** Affects typed deserialization into nullable value types expecting `null` for a gap.
- **`"[]"` and `"key: []"` now decode as an empty array instead of the literal string `"[]"`.**
- **Malformed array-length headers** (`[03]`, `[-1]`, `[bar]`) **now throw in strict mode** instead of being silently accepted or misparsed — **but now fall through to key-value parsing instead of throwing in non-strict mode** (spec-legal either way; previously always threw regardless of `Strict`).
- **Blank lines anywhere inside an array's row/item range now throw in strict mode** — previously only a blank line immediately after the header was detected; one between later rows/items was silently ignored even in strict mode.
- **NaN/+Infinity/-Infinity now encode as `null` instead of throwing** — a behavior change from "always throws" to "always succeeds," lower risk than the above but still a change in observable behavior for existing callers catching that exception.
- **Unrecognized string escape sequences now throw on decode instead of being silently passed through as literal text.**
- **Encoded number text is now the shortest round-trippable representation instead of always 17 significant digits (`G17`)**, and numbers in the `1e-6 .. 1e21` range never use exponential notation. The *decoded value* is unchanged, but the *encoded text* of many numeric values differs — relevant if anything compares raw TOON output rather than decoded values.
- **Value and key quoting changed** in both directions: strings with leading/trailing whitespace or starting with `-` are now correctly quoted (previously weren't — a real self-round-trip bug, since the decoder's own trimming silently stripped such padding back out); keys starting with a digit or containing a space are now quoted; conversely, strings that only *looked* numeric to `double.TryParse` (thousands separators, a leading `+`) or that merely contained a delimiter that wasn't actually active are no longer over-quoted.
- **CSV import (`Toon.DotNet.CSV`): cells with leading zeros (e.g. zip codes) are no longer coerced to numbers.** A CSV cell `"05678"` previously became the TOON number `5678`, silently losing the leading zero; it's now correctly encoded as the string `"05678"`.

### Fixed
All findings below are cross-referenced in [TOON_V3.md](TOON_V3.md)'s findings table.
- Decoding an empty document threw instead of returning `{}` (spec §5).
- `"[]"` (bare root) and `"key: []"` decoded as the literal string `"[]"` instead of an empty array (§4, §9.1).
- Encoding a non-uniform array containing a nested array-of-arrays as a list item silently dropped that item with no error (§9.2, §9.4) — the highest-severity finding in the audit, since it lost data with no signal at all.
- Empty tokens inside an inline/tabular array (e.g. `a,,c`) decoded to `null` instead of empty string `""` (§9.1).
- Numeric tokens with leading zeros ("05", "0001", "-05") silently decoded as numbers instead of strings (§4); "0.5", "0e1", and "-0.5" continue to correctly decode as numbers.
- Malformed array-length headers (`[03]`, `[-1]`) were silently accepted instead of rejected (§6).
- Duplicate sibling object keys always resolved via silent last-write-wins even in strict mode; strict mode now correctly errors (§8, §14.4).
- Encoding `NaN`/`Infinity`/`-Infinity` threw instead of converting to `null` (§3).
- A nested array header with no explicit delimiter suffix incorrectly inherited its parent array's delimiter instead of defaulting to comma (§6).
- When a tabular array was a list-item object's first field, both the encoder and decoder placed/expected rows at the wrong depth, colliding with the object's remaining fields (§10) — fixed symmetrically on both sides.
- `\uXXXX` unicode escape sequences were entirely unimplemented: other C0 control characters were emitted as raw, unescaped bytes on encode (a §15 security concern) instead of `\uXXXX`, and `\uXXXX` was not recognized at all on decode; an unrecognized escape sequence was silently passed through as literal text instead of being rejected (§7.1).
- Key quoting reused the value-quoting rule set (§7.2) instead of the stricter §7.3 unquoted-key identifier pattern (§7.3).
- Value quoting was missing two of the nine required §7.2 conditions (leading/trailing whitespace; a value equal to or starting with `-`).
- Value quoting ignored the configured delimiter entirely, always checking for comma, pipe, *and* tab regardless of which was actually active (§11.1) — `LiteralUtils.FormatPrimitive` already received a `delimiter` parameter but never forwarded it.
- The "numeric-like" quoting check used `double.TryParse` as a stand-in for the spec's exact regex, over-quoting thousands-separator/leading-`+` strings and under-quoting an all-digit string exceeding `double`'s range (§7.2).
- Doubles were formatted with `ToString("G17")` instead of the shortest round-trippable form, and could emit forbidden exponential notation inside the `1e-6..1e21` canonical range (§2).
- Blank-line-inside-array strict validation only checked the range immediately after the header line, missing blank lines between later rows/items (§12).
- A malformed bracket segment always threw unconditionally regardless of `Strict`; non-strict mode now falls through to key-value parsing as spec §14.2 permits.
- CRLF line endings were tolerated only incidentally via `.Trim()` calls scattered through downstream parsing rather than deliberately normalized at the scanner boundary (§1.2) — fragile against future refactors even though it worked correctly at the time.
- `Toon.DotNet.CSV`: `ToonCsv.ToCsv`'s CSV export formatted decoded numbers via `JsonElement.GetRawText()`, which reflected the decoder's internal `G17`-based storage rather than a clean value — decoding the TOON text `9.99` and exporting to CSV produced `9.9900000000000002`.

### Added
- `\uXXXX` unicode escape support on both encode (other C0 controls now escape correctly) and decode (case-insensitive hex digits; rejects fewer than 4 hex digits and surrogate code points per §7.1).
- `StringUtils.EscapeKey` — dedicated §7.3-compliant key quoting, separate from value quoting.
- New test file `tests/Toon.DotNet.Tests/ToonV3ComplianceTests.cs` — spec-compliance regression tests for every finding above, each cross-referenced to its TOON_V3.md finding number.
- New CRLF-handling tests in `ToonScannerTests.cs`; new leading-zero and number-formatting tests in `ToonCsvTests.cs`.

### Package versions in this release
- **Toon.DotNet** `1.7.4` → `3.3.2` (spec-aligned versioning scheme takes effect; see the note at the top of this file).
- **Toon.DotNet.CSV** `1.7.3` → `1.8.0` (own semver track; the leading-zero coercion fix is a behavior change for existing consumers).
- **Toon.DotNet.Excel** unchanged at `1.7.3` (no code changes in this release).
