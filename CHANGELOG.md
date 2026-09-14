# Changelog

All notable changes to this project will be documented in this file. The format is based on Keep a Changelog.

**Versioning:** as of v3.3.2, the **core `Toon.DotNet` package's version tracks the [TOON specification](https://github.com/toon-format/spec) version it implements**, rather than semantic versioning against its own prior release history — the current version, `4.1.1`, means "conforms to TOON spec v4.1.1" (with one documented exception, see the `4.1.1` entry below), not "the fourth major revision of this library's API." Compatibility-relevant changes are still called out explicitly under each release (see the "Breaking changes" notes below) since the version number itself no longer signals API stability the way semver does. The **`Toon.DotNet.CSV`** and **`Toon.DotNet.Excel`** integration packages are not implementations of the spec themselves and continue to follow ordinary semantic versioning on their own numbering track.

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

## [4.1.1] - 2026-09-14
The core library moves to the v4.x spec line: `Constants.SpecVersion` and the `Toon.DotNet` package version both move from `3.3.2` to `4.1.1`, and `EncodeOptions.SpecVersion`'s default flips from `V3` to `V4`. This is the culmination of the TOON_V4.md work plan (phases 0–5) — see that document for the full phase-by-phase history, including two design corrections made along the way (the real spec text made full-line comments unconditional rather than an opt-in flag as originally guessed; the "tabular key reordering" and "prototype keys" gap-list rows turned out to already be non-issues for this codebase) and one user-directed scope change (making `EncodeOptions.SpecVersion` actually gate output, rather than shipping v4-only encoder behavior unconditionally).

### Breaking changes
As with the `3.3.2` release, the version number doesn't map to a semver major bump the way a from-scratch API redesign would — read this section rather than relying on the version jump alone.
- **`EncodeOptions.SpecVersion` now defaults to `V4` instead of `V3`.** `Toon.Encode(data)` with no options can now produce different output than before for any input containing: an array of objects with a nested-uniform object column (now a nested field group tabular header, e.g. `id,customer{name,country}`, instead of falling back to list form); an object whose values are all uniform objects (now keyed tabular form, e.g. `users[2:]{age,city}:`, instead of ordinary nested-object form); or a string value matching the numeric-like pattern with a leading `+` (e.g. `"+5"`, now quoted instead of emitted bare). Pass `new EncodeOptions { SpecVersion = ToonSpecVersion.V3 }` to keep the old (v3.3.2) output shape.
- **Decoding gained three strict-mode-independent tightenings** (already shipped ahead of this release in the phase 3/4 work, called out again here since this is the release that surfaces them under the new version number): indentation depth-jump/over-indent/trailing-content errors, UTF-8 well-formedness rejection on byte-level decode paths, and the v4.1.0 misplaced-scalar rule now erroring in non-strict mode too (previously tolerated). The latter two are opt-out via `DecodeOptions.LegacyCompatibility` where a genuine v3/v4 semantic conflict exists (token trimming scope, misplaced-scalar tolerance); the indentation and UTF-8 tightenings have no opt-out, since they were v3.3.2 conformance gaps rather than v3/v4 conflicts.

### Added
- Full-line `#` comments (spec §5.1), nested field groups in tabular headers (§9.3, RFC #46), and keyed tabular form for objects (§9.5, RFC #57) — see TOON_V4.md phase 2.
- `EncodeOptions.SpecVersion` (`ToonSpecVersion.V3`/`.V4`, default `.V4`) and `DecodeOptions.LegacyCompatibility` (`bool`, default `false`) — see TOON_V4.md phases 1 and 4.
- Strict-mode UTF-8 well-formedness rejection on byte-level `Stream` decode paths, indentation depth-jump/over-indent/trailing-content detection, and the v4.1.0 misplaced-scalar rule — see TOON_V4.md phase 3.

### Known caveat
- The decoder's number tokenization has not been fully audited against spec §4's normative number grammar, and out-of-range numeric literal handling is unverified. Leading-plus numeric-like string quoting (§7.2) is fixed and version-gated; the rest of the §4 grammar audit remains open, tracked in TOON_V4.md's "Number grammar" gap-list row. Shipping with this known, documented gap (rather than blocking the v4 default flip on it) was an explicit choice made when moving `Constants.SpecVersion` to `4.1.1`.

### Package versions in this release
- **Toon.DotNet** `3.3.2` → `4.1.1` (spec-aligned versioning; see the note at the top of this file).
- **Toon.DotNet.CSV** and **Toon.DotNet.Excel** unchanged in this entry — see the `Toon.DotNet.CSV 3.4.0` entry below for that package's own (unrelated, semver-tracked) release.

## [Toon.DotNet.Excel 3.4.0] - 2026-09-14
An Excel-package-only release (core `Toon.DotNet` is `4.1.1`, unrelated to this entry). Fixes a real fidelity gap found during TOON_V4.md phase 6 verification ("does the Excel integration still round-trip correctly against v4 output — actually check the code paths, don't assume inheritance"): decoding a TOON document that contains an object-of-uniform-objects value (spec §9.5 keyed tabular form, e.g. `Sales[2:]{age,city}:`, now the *default* core encoder output since the `4.1.1` release) previously collapsed an entire sheet's worth of tabular data into unreadable raw JSON text in a single cell, instead of a proper worksheet.

### Fixed
- `ToonExcel.Decode`/`LoadToonFile`/`SaveAsExcel`/`ConvertToonToExcel` (and their async counterparts) now render an object-of-objects value as a proper keyed worksheet: an entry per row, with a leading `key` column holding each entry's name, followed by one column per field (columns derived from the first entry's keys, mirroring the existing array-of-objects leniency — a later entry missing a field just leaves that cell blank rather than erroring).
- This is a general `JsonElement`-shape improvement, not v4-syntax-specific detection: TOON's decoded output can't distinguish "written via keyed-tabular-form syntax" from "written via plain nested §8 object syntax" with the same shape, so this also improves decoding of existing v3-authored documents with this shape, not just new v4 output.
- Deliberately **not** changed: root-level object dispatch (a root TOON object's top-level keys becoming one worksheet per key) is untouched, since that convention and "this whole object is one keyed dataset" are two genuinely conflicting interpretations of the same root shape — see the `Decode_RootKeyedTabularToon_StillTreatsTopLevelKeysAsSheetNames` test for the documented boundary of this fix.
- 8 new unit tests (109 total, up from 101).

### Verification note
- `Toon.DotNet.CSV` was also checked during this phase and found to already handle v4 output correctly with no code changes needed: nested field group cell values degrade gracefully to properly CSV-quoted raw JSON text (an inherent, pre-existing limitation of flat CSV, not a v4 regression), and the multi-dataset methods (`ToCsvDictionary`/`FromCsvDictionary`/etc., added in `Toon.DotNet.CSV 3.4.0`) are structurally safe from misinterpretation since their dictionary values are always arrays, never objects.

## [Toon.DotNet.CSV 3.4.0] - 2026-09-14
A CSV-package-only release (core `Toon.DotNet` is unchanged at `3.3.2`). Adds multi-dataset support to `Toon.DotNet.CSV`: CSV has no native multi-table concept, so a TOON document with several named datasets (a root object whose top-level values are each an array — the CSV equivalent of `Toon.DotNet.Excel`'s one-worksheet-per-dataset convention) now converts to one CSV per dataset, the same convention database/BI export tools use, rather than the previous hard failure.

### Added
- `ToonCsv.ToCsvDictionary(string toon, DecodeOptions?)` — converts a multi-dataset TOON document to a `Dictionary<string, string>` (dataset name → CSV content).
- `ToonCsv.ToCsvFiles(string toon, string outputDirectory, DecodeOptions?)` and its async counterpart `ToCsvFilesAsync` — write one `.csv` file per dataset to a directory (created if missing), with filesystem-safe name sanitization and numeric-suffix de-duplication when two dataset keys sanitize to the same file name.
- `ToonCsv.FromCsvDictionary(IReadOnlyDictionary<string, string> csvByName, EncodeOptions?)` — the reverse: combines named CSV content into one multi-dataset TOON document (always a root object, even for a single entry — it never unwraps to a bare root array).
- `ToonCsv.FromCsvFiles(string inputDirectory, EncodeOptions?, string searchPattern = "*.csv")` and its async counterpart `FromCsvFilesAsync` — the reverse: reads every matching CSV file in a directory and combines them into one multi-dataset TOON document, keyed by file name (processed in deterministic, ordinal file-path order).
- `ToonCsv.SaveCsvFilesAsToon`/`SaveCsvFilesAsToonAsync` — convenience wrappers that read a directory of CSV files and save the combined result directly to a `.toon` file.
- 30 new unit tests covering all of the above, including sanitization/de-duplication, empty/missing-directory handling, and round-trip fidelity through `ToCsvFiles` → `FromCsvFiles`.

### Design notes
- `ToCsvDictionary`/`ToCsvFiles` require the TOON root to be an object (one array per dataset); a root array (a single, unnamed dataset) is intentionally out of scope for these — use the existing `ToCsv`/`FromCsv` methods for that single-dataset case, rather than inventing a default dataset-name convention.
- A single-file, section-marker-delimited CSV convention was considered and rejected in favor of one-file-per-dataset: it would be non-standard, so most CSV consumers (pandas, Excel import, etc.) wouldn't understand it without bespoke pre-processing — defeating the point of emitting plain CSV.

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
