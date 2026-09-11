# Toon V4 Assessment

## Summary
Toon V4 should be treated as a compatibility and feature-completion milestone for the existing .NET implementation rather than a greenfield rewrite. The repository already contains a working core serializer, CSV and Excel integration packages, and a substantial xUnit suite, so the work should focus on finishing v4 semantics in the core parser/encoder, keeping the package surface stable, and validating behavior across the current multi-targeting matrix.

## Scope
- Core library: [src/Toon.DotNet/Toon.cs](src/Toon.DotNet/Toon.cs), [src/Toon.DotNet/ToonAsync.cs](src/Toon.DotNet/ToonAsync.cs), [src/Toon.DotNet/ToonStream.cs](src/Toon.DotNet/ToonStream.cs), plus the parser and encoder folders under [src/Toon.DotNet/Decode](src/Toon.DotNet/Decode) and [src/Toon.DotNet/Encode](src/Toon.DotNet/Encode).
- Shared types and constants: [src/Toon.DotNet/Types.cs](src/Toon.DotNet/Types.cs), [src/Toon.DotNet/Constants.cs](src/Toon.DotNet/Constants.cs), and the shared helpers in [src/Toon.DotNet/Shared](src/Toon.DotNet/Shared).
- Integration packages: [src/Toon.DotNet.CSV/ToonCsv.cs](src/Toon.DotNet.CSV/ToonCsv.cs) and [src/Toon.DotNet.Excel/ToonExcel.cs](src/Toon.DotNet.Excel/ToonExcel.cs).
- Test coverage: [tests/Toon.DotNet.Tests](tests/Toon.DotNet.Tests) with focused files such as [tests/Toon.DotNet.Tests/ToonParserTests.cs](tests/Toon.DotNet.Tests/ToonParserTests.cs), [tests/Toon.DotNet.Tests/ToonEncoderTests.cs](tests/Toon.DotNet.Tests/ToonEncoderTests.cs), [tests/Toon.DotNet.Tests/ToonDecoderTests.cs](tests/Toon.DotNet.Tests/ToonDecoderTests.cs), [tests/Toon.DotNet.Tests/StreamTests.cs](tests/Toon.DotNet.Tests/StreamTests.cs), [tests/Toon.DotNet.Tests/ToonCsvTests.cs](tests/Toon.DotNet.Tests/ToonCsvTests.cs), and [tests/Toon.DotNet.Tests/ToonExcelTests.cs](tests/Toon.DotNet.Tests/ToonExcelTests.cs).

## Spec baseline
✅ **Pinned.** [`Constants.SpecVersion`](src/Toon.DotNet/Constants.cs) = `"3.3.2"`, with a matching README note and package version (see [Versioning](README.md#versioning)) — this was phase 0 of the implementation order below. Future version bumps (e.g. moving to the v4.x line) now have a fixed, discoverable starting point instead of being inferred from behavior.

**Status:** at the time this document was first drafted, the implementation predated even [toon-format/spec](https://github.com/toon-format/spec) v3.1.0 — it was missing `\uXXXX` unicode escapes (a v3.1.0 feature) on top of everything in the gap list below. [TOON_V3.md](TOON_V3.md) audited the full v3.0.x–v3.3.2 rule set separately from this document and found (and fixed) all 21 gaps against that baseline, including the missing `\uXXXX` support — **the v3 line is fully closed**, which is what let the pinned baseline above become `3.3.2` rather than an earlier or partial version. Everything in this document's gap list (all v4.0.0+ features: comments, key-folding removal, keyed-tabular objects, nested field groups, and the rest) remains unimplemented and is the actual subject of this document.

One v3 item is still relevant here despite being "fixed": the document-vs-active delimiter-scoping finding was evaluated three ways and deliberately scoped down during the v3 work — a **minimal fix** (value quoting now respects the actually-configured delimiter instead of unconditionally quoting for comma/pipe/tab) shipped; **full per-array delimiter selection** — a genuinely new `EncodeOptions` API surface letting one `Encode()` call use different delimiters for different nested arrays — was evaluated but explicitly deferred, not designed. If this v4 work ends up needing real per-array delimiter selection (e.g. for `SpecVersion`-gated per-array behavior), that design still needs to happen here first; the minimal fix does not provide it.

## v4.0.0 → v4.1.1 gap list
Concrete gaps against the upstream spec, drawn from the v4.0.0, v4.1.0, and v4.1.1 release notes. Each should get its own implementation task and regression tests rather than being folded into a single "v4 support" change.

| Area | Spec change | Current state | Gap |
|---|---|---|---|
| Comments | v4.0.0 makes full-line `#` comments normative, with defined line classification | ✅ **Done (phase 2 step 5).** Unconditional lexical pre-pass in [ToonScanner.cs](src/Toon.DotNet/Decode/ToonScanner.cs); no `AllowComments` flag needed (real spec text makes this unconditional, correcting this table's original guess) | None |
| Key folding / path expansion | v4.0.0 removes these entirely (breaking) | Not present in [Normalizer.cs](src/Toon.DotNet/Encode/Normalizer.cs) or the decoder (confirmed via search) | Likely a non-issue; explicitly note in release docs that this repo never implemented folding, so no removal work is needed |
| Tabular headers: nested field groups | v4.0.0 adds nested field groups in tabular headers (RFC #46) | ✅ **Done (phase 2 step 6).** `TabularField` tree type + recursive detect/encode/decode in [ToonEncoder.cs](src/Toon.DotNet/Encode/ToonEncoder.cs) / [ToonDecoder.cs](src/Toon.DotNet/Decode/ToonDecoder.cs) / [ToonParser.cs](src/Toon.DotNet/Decode/ToonParser.cs) | None |
| Tabular form for objects | v4.0.0 adds keyed tabular form for objects (RFC #57) | ✅ **Done (phase 2 step 7).** New keyed-seg header grammar + `DecodeKeyedTabularObject`/`ExtractKeyedTabularHeader`/`EncodeObjectAsKeyedTabular` | None |
| Number grammar | v4.0.0 defines a normative decoder number grammar (§4); leading-plus numeric-like strings require quoting (§7.2); out-of-range numbers may error | Current number parsing in the scanner/decoder predates this | Audit number tokenization against §4/§7.2 and add quoting/error-path tests (phase 4, leading-plus quoting) |
| Unquoted keys | v4.0.0 defines the decoder unquoted-key token rule (§7.4); pins strict acceptance of unquoted required-quote values | Needs audit against current key-token scanning | Verify/align unquoted-key acceptance rules (phase 3) |
| Indentation | v4.0.0 makes indentation depth jumps a strict-mode error; over-indented and trailing root lines must no longer be silently discarded | `options.Strict` exists in [ToonDecoder.cs](src/Toon.DotNet/Decode/ToonDecoder.cs) / [ToonScanner.cs](src/Toon.DotNet/Decode/ToonScanner.cs) but depth-jump and over-indent handling need verification against the new rules | Audit strict-mode indentation validation; add negative test cases (phase 3) |
| Empty list items | v4.0.0 allows bare `[]` bracket pairs as empty inner-array list items | ✅ **Done (phase 2 step 4)** — already satisfied by the v3 empty-array-literal fix, confirmed and pinned with regression tests | None |
| UTF-8 handling | v4.0.0 requires strict decoders to reject ill-formed UTF-8 byte input | Needs audit of stream/byte-based decode paths ([ToonStream.cs](src/Toon.DotNet/ToonStream.cs)) | Verify byte-level input validation in strict mode (phase 3) |
| Prototype keys | v4.0.0 makes prototype-key handling normative (§15) | ✅ **Done (phase 2 step 9)** — confirmed already safe by construction (`Dictionary<string, JsonElement>` has no prototype chain to pollute) across all key positions | None |
| Tabular key reordering | v4.0.0 exempts tabular key reordering from the §2 round-trip predicate | ✅ **Done (phase 2 step 8)** — confirmed to be a non-issue (no `Diff`/equality-comparison API exists in this codebase); tabular detection already tolerates per-row key reordering | None |
| Token trimming | v4.0.0 restricts token trimming to U+0020 only (promoted to MUST) | Needs audit — current trimming may use `string.Trim()` defaults, which trim a broader whitespace set than U+0020 | Audit trimming calls in the scanner/normalizer for over-broad whitespace handling (phase 4) |
| Malformed headers | v4.0.0 classifies length-less bracket segments as malformed headers | ✅ **Done (phase 2 step 10)** — already satisfied by prior v3 work, confirmed and pinned with regression tests | None |
| Misplaced scalars | v4.1.0 makes a misplaced scalar line an error in **both** strict and non-strict modes (previously only strict) | Current non-strict path likely tolerates this | This is a behavioral tightening, not additive — decide whether to treat it as a breaking fix and document it as such (phase 4) |

Two items in this list are genuinely **breaking** relative to "no breaking changes" in the Compatibility strategy below: the v4.1.0 misplaced-scalar rule (a previously-accepted non-strict input becomes an error) and, if any downstream consumer relied on lenient token trimming, the U+0020-only trimming rule. These should be called out explicitly in the changelog rather than silently folded into a "feature release."

## Version-aware `EncodeOptions` / `DecodeOptions`

### Recommendation
Add an explicit `SpecVersion` to `EncodeOptions` (safe, deterministic — the encoder fully controls its own output). For `DecodeOptions`, do **not** add content-sniffing auto-detection as default behavior. Implement the decoder against the full v4 grammar as a superset of v3 wherever the two are compatible, and add narrow, explicit opt-in flags only for the handful of places where v3 and v4 genuinely disagree on the same input. Auto-detection is rejected as a default because part of the v4 delta is a value-level semantic change, not a structural one — the risk is a silently wrong decoded value, not a parse error, and that failure mode is worse than requiring the caller to state intent for a library whose value proposition includes round-trip fidelity.

### `EncodeOptions.SpecVersion` — safe to add
- New `SpecVersion` enum (`V3`, `V4`), defaulting to `V4` once v4-only encoder features (keyed tabular form, nested field groups) are implemented.
- No ambiguity: the encoder is the sole author of its output, so this is a pure additive option in the same family as `Delimiter` / `LengthMarker`.
- Changing the *default* to `V4` changes emitted bytes even though the API signature doesn't change — call this out explicitly in the changelog as a behavior change, not just an additive feature.

### `DecodeOptions`: superset grammar, not per-version detection
- Most of the v4 gap list (comments, keyed tabular form, nested field groups, bare `[]` empty items, malformed-header classification, tabular key-reordering exemption, prototype-key handling) is purely additive grammar. A decoder that implements v4 fully parses v3 documents correctly automatically, because v3 is a subset — no version flag, no detection, and no impact to the caller. This is the mechanism that actually delivers "works for both without caller effort," not sniffing.
- `DecodeOptions.Strict` already exists and already defaults to `true`. Several v4 "strict-mode" tightenings (indentation depth-jump errors, the unquoted-key strict acceptance rule, UTF-8 well-formedness rejection) land inside that existing flag. **Implementing them changes decode results for today's default-options callers without adding any new option at all** — this is the single biggest compatibility risk in this plan, bigger than anything a new flag would introduce. Call it out explicitly in the changelog and in the "breaking changes" note above, independent of the `SpecVersion` design.
- A small set of changes are genuine **semantic conflicts** that can't be resolved by "understand more grammar" or by the existing `Strict` flag, because the same input is either accepted-with-different-meaning or valid-before/invalid-after regardless of strictness:
  - Token trimming scope (U+0020-only vs. broader whitespace) — same bytes decode to a different string.
  - Leading-plus numeric-like strings requiring quoting (§7.2) — previously-unquoted `+5` changes meaning or becomes an error.
  - Misplaced scalar line becoming an error in v4.1 in *both* strict and non-strict modes — previously-tolerated non-strict input starts throwing.

  These three need an explicit, caller-visible opt-in — e.g. `DecodeOptions.LegacyCompatibility` (bool, default `false`) or a `DecodeOptions.SpecVersion` enum mirroring the encoder's — defaulting to correct v4 behavior, with legacy/lenient behavior available only when a caller deliberately asks for it. Never select between them by inspecting the input.

### Rejected: content-sniffing auto-detect as the default
Auto-detecting "is this v3 or v4" by parsing and guessing is rejected as a default because:
1. It's unnecessary for most of the gap list (superset grammar already handles it).
2. For the semantic-conflict slice, there is no reliable signal to sniff — a document valid under both trimming interpretations gives no clue which one the author meant, so "detection" there is really an arbitrary tie-break dressed up as intelligence.
3. The failure mode is silent data corruption (a wrong decoded value returned successfully) rather than a caught error — strictly worse than requiring the caller to state a flag, given the library's round-trip guarantees (`Diff`, `SizeComparison`, round-trip helpers).

A best-effort two-pass fallback (attempt strict v4, retry with `LegacyCompatibility` on failure) is acceptable as an **opt-in** convenience only (e.g. `SpecVersion.Auto`), reusing the existing `TryDecode` plumbing pattern — but it must be documented as best-effort and explicitly noted as unable to disambiguate the trimming case, not marketed as reliable version detection.

## Repo-specific implementation areas
1. Core parser and encoder behavior
   - Work through the [gap list](#v400--v411-gap-list) above rather than a general "confirm v4 parses correctly" pass — each row needs its own fix/audit and test cases.
   - Review the shared normalization and formatting logic in the core library before changing the public entry points.
2. Public API and compatibility
   - Preserve the existing public API in [src/Toon.DotNet/Toon.cs](src/Toon.DotNet/Toon.cs) and avoid introducing breaking changes in the core `Toon` entry points.
   - Add `SpecVersion` to `EncodeOptions` and the `LegacyCompatibility` (or equivalent) opt-in to `DecodeOptions` per the [Version-aware EncodeOptions / DecodeOptions](#version-aware-encodeoptions--decodeoptions) section — additive to the API surface, but treat any default-behavior change (v4-default encoding, strict-mode tightening under the existing `Strict=true` default) as a documented behavior change, not silently bundled into "no breaking changes."
   - Do not implement decode-side version auto-detection as default behavior; see the rejection rationale above.
   - Keep the current multi-target approach intact: .NET 8/9/10/11 and .NET Standard 2.0 compatibility remain important for downstream consumers.
3. Integration surfaces
   - Ensure the CSV and Excel extensions still round-trip correctly with the core library after any v4 behavior changes.
   - Keep package-level docs and release notes aligned with the changes so the NuGet packages remain predictable.
4. Documentation and examples
   - Update [README.md](README.md), [CHANGELOG.md](CHANGELOG.md), and package-specific docs in [src/Toon.DotNet.CSV/README.md](src/Toon.DotNet.CSV/README.md) and [src/Toon.DotNet.Excel/README.md](src/Toon.DotNet.Excel/README.md) as needed.

## Compatibility strategy
- Keep the implementation compatible with the current target frameworks declared in [src/Toon.DotNet/Toon.DotNet.csproj](src/Toon.DotNet/Toon.DotNet.csproj), [src/Toon.DotNet.CSV/Toon.DotNet.CSV.csproj](src/Toon.DotNet.CSV/Toon.DotNet.CSV.csproj), and [src/Toon.DotNet.Excel/Toon.DotNet.Excel.csproj](src/Toon.DotNet.Excel/Toon.DotNet.Excel.csproj).
- Retain the existing `#if !NETSTANDARD2_0` guards where needed for newer APIs and avoid relying on .NET-only features that would weaken the netstandard2.0 story.
- Prefer additive changes to `EncodeOptions` and `DecodeOptions` over API removal or signature churn, because the current codebase already exposes a broad surface area. This applies to the *shape* of the options; some *default behavior* under those options is still expected to change (see below) — additive API and unchanged behavior are not the same guarantee, and the plan should not conflate them.
- Distinguish API surface stability (keep) from parse-acceptance/decoded-value behavior (may legitimately change): the real v4 spec ships breaking changes — full-line comments, the v4.1 misplaced-scalar rule, U+0020-only trimming, and stricter validation under the existing `Strict=true` default. Do not resolve this tension by adding decode-side auto-detection; resolve it by defaulting to correct v4 behavior and exposing an explicit, documented `LegacyCompatibility` opt-in (see [Version-aware EncodeOptions / DecodeOptions](#version-aware-encodeoptions--decodeoptions)) for callers who need the old lenient behavior.
- Keep the existing streaming, async, and file-based operations working for the current targets so that v4 support does not regress the broader serializer experience.

## Testing plan
- Extend the xUnit suite in [tests/Toon.DotNet.Tests](tests/Toon.DotNet.Tests) with focused v4 parser, encoder, and round-trip cases rather than relying on a single integration test.
- Cover the main paths already exercised in the existing test classes: basic serialization, delimiters, stream and text-reader/writer behavior, file operations, CSV conversion, and Excel conversion.
- Preserve the current regression baseline and ensure the suite remains green on the repo’s supported target frameworks.

## Docs and release planning
- Deliver Toon V4 work in two stages: first the parser/encoder changes plus regression tests, then package/docs/release updates.
- Treat the release as a minor or feature release rather than a breaking change unless the compatibility review proves otherwise.
- Include a concise changelog entry describing the v4 support status, compatibility impact, and any known caveats.

## Acceptance criteria
- Toon v4 examples encode and decode correctly through the core library without introducing regressions in existing behavior.
- Existing public APIs in the core library remain callable for the current target frameworks.
- `EncodeOptions.SpecVersion` defaults to `V4` and produces deterministic, version-appropriate output with no ambiguity.
- `DecodeOptions` parses v3 and v4 documents correctly without a version flag wherever the grammars are compatible (superset parsing); no default-path behavior silently reinterprets ambiguous input based on content sniffing.
- The three identified semantic-conflict rules (token trimming scope, leading-plus quoting, misplaced-scalar strictness) are gated behind an explicit, documented opt-in rather than inferred, and default to correct v4 behavior.
- Any behavior change reachable through the *existing* default options (notably `Strict=true` tightening) is documented in the changelog even though no new option was added.
- CSV and Excel integrations continue to work with the same round-trip expectations as before.
- The test suite passes with new v4-specific cases added — including negative tests proving the library does not silently misdecode the semantic-conflict cases — and the documentation reflects the implementation status clearly.
- The release notes state the supported v4 scope and any compatibility caveats for downstream users.

## Recommended implementation order
Expanded to one concrete, individually testable step per gap-list row, same granularity as [TOON_V3.md](TOON_V3.md)'s fix order.

### Phase 0 — Groundwork
1. ✅ **Done.** Pin the spec baseline explicitly (`Constants.SpecVersion`, README note, package version) — see [Spec baseline](#spec-baseline) above.

### Phase 1 — Inert API surface (no behavior change yet)
2. ✅ **Done.** Added `EncodeOptions.SpecVersion` (`ToonSpecVersion` enum: `V3`/`V4`), defaulting to `V3` — see [Types.cs](src/Toon.DotNet/Types.cs). No behavior wired up yet; a stable place for phase 5 to branch on. Covered by `EncodeOptions_SpecVersion_DefaultsToV3` / `_IsCurrentlyInert` in [ToonV4ComplianceTests.cs](tests/Toon.DotNet.Tests/ToonV4ComplianceTests.cs) — the inert test is expected to start failing once phase 5 lands, at which point it should be replaced with real v4-output-difference tests.
3. ✅ **Done.** Added `DecodeOptions.LegacyCompatibility` (`bool`, default `false`) — see [Types.cs](src/Toon.DotNet/Types.cs). Resolved the open design question in favor of a bool over a mirrored `SpecVersion` enum: the decoder should always understand the full v4 superset grammar, and a `SpecVersion` enum on the decode side risks being misread as "reject v4 syntax when set to V3." A narrow bool matches "explicit opt-in only for genuine conflicts" more precisely. Also inert initially — covered by `DecodeOptions_LegacyCompatibility_DefaultsToFalse` / `_IsCurrentlyInert`, expected to start failing once phase 4 lands.

### Phase 2 — Additive/superset grammar (safe, no flag needed) — ✅ Done
`\uXXXX` escape handling is already done as a side effect of the v3 work (TOON_V3.md fix-order item 8), so it's dropped from this list. All items below are implemented and covered in [ToonV4ComplianceTests.cs](tests/Toon.DotNet.Tests/ToonV4ComplianceTests.cs); full suite green at 720/720 with no regressions after this phase.

4. ✅ **Done — already satisfied by prior v3 work, no new code needed.** Bare `[]` as an empty inner-array list item. `LiteralUtils.ParsePrimitiveToken("[]")` (the v3 empty-array-literal fix) already returns an empty JSON array, and `DecodeListArray`'s primitive branch reaches it whenever a list item has no colon — confirmed equivalent to the explicit `- [0]:` form. Covered by `Decode_BareEmptyBracketListItem_DecodesAsEmptyInnerArray` / `_EquivalentToExplicitZeroLengthForm`.
5. ✅ **Done.** Full-line `#` comments (spec §5.1). Implemented as an unconditional lexical pre-pass in [ToonScanner.cs](src/Toon.DotNet/Decode/ToonScanner.cs) — a line whose first non-space character is `#` is dropped entirely before any other processing, in strict and non-strict mode alike. **Correction to this plan's original wording**: real spec text (fetched from the upstream spec repo during implementation) makes comment stripping *normative and unconditional*, not gated behind an `AllowComments` option as originally guessed here — so no new `DecodeOptions` flag was added. On the encode side, `StringUtils.ShouldQuoteString` already quotes any string containing `#` anywhere (broader than the spec's "equals or starts with `#`" MUST-quote rule, but safely so — over-quoting is spec-compliant and was already in place before this phase). Covered by `Decode_FullLineComment_IsStrippedFromDocument` and five related tests (indented comments, non-strict mode, `#` mid-line as ordinary content, quoted `#`-leading values, and the encoder quoting/round-trip pair).
6. ✅ **Done.** Nested field groups in tabular headers (RFC #46), e.g. `orders[2]{id,customer{name,country},total}:`. New internal `TabularField` tree type (`Types.cs`) replaces the old flat `string[]` field list. Encoder: `ExtractTabularHeader`/`BuildTabularFieldOrNull` (`ToonEncoder.cs`) recursively detect nested-uniform columns (every value a non-empty object, same key set, sub-columns themselves uniform-primitive or nested-uniform, unbounded depth) and `Primitives.FormatHeader`/`FormatField` render them recursively. Decoder: `ToonParser.ParseFieldList`/`ParseFieldSegment`/`FindMatchingCloseBrace` parse nested groups (brace-depth-aware, so a group's own commas/close-brace aren't mistaken for the outer list's), and `ToonDecoder.BuildTabularRowElement`/`CountLeafFields` do the depth-first pre-order cell walk in both directions. Added the strict-mode per-row cell-count check required by spec §9.3 that was missing before this phase (previously silently truncated/ignored mismatched row widths). Two pre-existing tests (`ToonEncoderTests.cs`) that asserted the old fallback-to-list behavior for nested object columns were updated to the correct v4 nested-field-group tabular output — verified via direct execution that the new output round-trips exactly. Covered by 7 tests including deep (3-level) nesting and a non-uniform-column fallback-to-list-form check.
7. ✅ **Done.** Keyed tabular form for objects (RFC #57), e.g. `users[2:]{age,city}:` + `alice: 30,Berlin` rows. New `keyed-seg` bracket grammar (`[N:delim?]`, literal colon inside the brackets) detected in `ToonParser.ParseBracketSegment`/propagated via new `ArrayHeaderInfo.IsKeyedTabular`; field list is required (malformed otherwise, same strict-throw/non-strict-fall-through convention as the length-less-bracket case). Decoder: new `ToonDecoder.DecodeKeyedTabularObject`, wired in at both the root-value and object-field dispatch points (reuses `BuildTabularRowElement` for the per-entry cell walk); entry-row line classification follows §9.5's own rule (any unquoted-colon line at entry depth is a row; no colon-vs-delimiter disambiguation), decoders accept N≥0 even though encoders never emit N<2. Encoder: new `ToonEncoder.ExtractKeyedTabularHeader`/`EncodeObjectAsKeyedTabular` and `Primitives.FormatKeyedHeader`, wired into object-field encoding and root-object encoding only — **not** list-item encoding, per spec §10 ("array elements are anonymous and never encode in keyed tabular form"). Covered by 10 tests including root-position, zero-entries, single-entry (correctly falls back to nested form), strict-mode cell-count and missing-field-list errors, and the array-elements-never-qualify guard.
8. ✅ **Done — confirmed to be a non-issue for this codebase, no code change needed.** Tabular key-reordering exemption. This codebase has no `Diff`/equality-comparison API at all (the "round-trip/`Diff` comparison" this plan originally referenced doesn't exist here — that was a guess based on the upstream JS reference implementation, not a fact about this repo), so there was nothing to loosen. Verified empirically instead: tabular-detection already only checks that each row has the *same key set* as the header (`AllRowsHaveExactKeySet`), never key *order*, and decoding always rebuilds each row by walking the header's field list rather than the input's property order — already exactly what spec §9.3's "order per object MAY vary" and §2's reordering exemption require, as a side effect of how step 6 was built. Covered by `Encode_ArrayOfObjectsWithDifferentKeyOrderPerRow_StillUsesTabularForm` and a round-trip test proving the decoded order normalizes to the header's order regardless of each row's original property order.
9. ✅ **Done — confirmed already safe, no code change needed.** Prototype-key handling (§15: `__proto__`, `constructor`, `prototype` must decode as ordinary own entries with no mutation of shared host-object-model state). Safe by construction for this .NET decoder: decoded objects are built as `Dictionary<string, JsonElement>`, which has no prototype chain for a dangerous key name to pollute (a JS-specific attack surface that doesn't exist in .NET), and `System.Text.Json` gives these names no special treatment. Verified empirically across every key position the grammar supports — plain object fields, tabular field names, and keyed-tabular entry keys — plus a round-trip fidelity check. Covered by 4 tests in `ToonV4ComplianceTests.cs`.
10. ✅ **Done — already satisfied by prior v3 work, no new code needed.** Malformed-header classification (length-less bracket segments, e.g. `items[]:`). Resolved the second open design question (`Strict`-gated vs. unconditional) empirically: `ParseBracketSegment`/`ParseArrayHeaderLine` (TOON_V3.md items 6/7/20) already throw `InvalidOperationException` for empty bracket content in strict mode (the default) and already fall through to plain key-value parsing in non-strict mode — i.e. it follows the same `Strict`-gated pattern as the v3 malformed-bracket fix. Covered by `Decode_LengthLessBracketSegment_StrictMode_Throws` / `_NonStrictMode_FallsThroughToKeyValueParsing` in [ToonV4ComplianceTests.cs](tests/Toon.DotNet.Tests/ToonV4ComplianceTests.cs).

### Phase 3 — Strict-mode tightenings (land inside the *existing* `Strict` flag)
The single biggest compatibility risk in this plan — changes default-caller behavior with zero new options:
11. Indentation depth-jump strict-mode error.
12. Unquoted-key strict acceptance rule (§7.4).
13. UTF-8 well-formedness rejection in strict mode (byte-level decode paths — `ToonStream.cs`).

Each needs a "this used to silently succeed, now correctly throws under default `Strict=true`" regression test, and an explicit changelog callout — same pattern as TOON_V3.md's duplicate-key fix.

### Phase 4 — Genuine semantic conflicts (gated by the new `LegacyCompatibility` opt-in)
Not resolvable by grammar alone or by `Strict` alone:
14. Token trimming scope (U+0020-only vs. broader whitespace).
15. Leading-plus numeric-like strings requiring quoting (§7.2).
16. Misplaced scalar line becoming an error in *both* strict and non-strict modes (v4.1).

Each needs two tests: old behavior still reachable via `LegacyCompatibility=true`, new (correct) behavior is default.

### Phase 5 — Flip the encoder default
17. Once all v4-only encoder features (steps 6–7) are implemented and tested, flip `EncodeOptions.SpecVersion`'s default to `V4`. Deliberate, documented behavior change — changes emitted bytes, not just API shape.

### Phase 6 — Integration and docs
18. Verify `Toon.DotNet.CSV`/`Toon.DotNet.Excel` still round-trip correctly against v4 output — actually check their code paths, not just assume inheritance (the v3 work found two real CSV-specific bugs this way that weren't caught by assuming the integration packages "just inherit" core fixes).
19. Update `README.md`, `CHANGELOG.md`, and this document's status as each phase lands.