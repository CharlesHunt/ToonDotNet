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
| Comments | v4.0.0 makes full-line `#` comments normative, with defined line classification | `AllowComments` is listed as an unimplemented option in [BACKLOG.md](BACKLOG.md); no comment handling in [ToonScanner.cs](src/Toon.DotNet/Decode/ToonScanner.cs) | Implement comment-line recognition in the scanner and thread `AllowComments` through `DecodeOptions` |
| Key folding / path expansion | v4.0.0 removes these entirely (breaking) | Not present in [Normalizer.cs](src/Toon.DotNet/Encode/Normalizer.cs) or the decoder (confirmed via search) | Likely a non-issue; explicitly note in release docs that this repo never implemented folding, so no removal work is needed |
| Tabular headers: nested field groups | v4.0.0 adds nested field groups in tabular headers (RFC #46) | No nested-header handling in [ToonEncoder.cs](src/Toon.DotNet/Encode/ToonEncoder.cs) / [ToonDecoder.cs](src/Toon.DotNet/Decode/ToonDecoder.cs) | New encoder + decoder feature; needs header-grammar changes and round-trip tests |
| Tabular form for objects | v4.0.0 adds keyed tabular form for objects (RFC #57) | Tabular support in `ToonEncoder.cs`/`ToonDecoder.cs` only covers arrays of objects, not keyed-object tabular form | New encoder + decoder feature |
| Number grammar | v4.0.0 defines a normative decoder number grammar (§4); leading-plus numeric-like strings require quoting (§7.2); out-of-range numbers may error | Current number parsing in the scanner/decoder predates this | Audit number tokenization against §4/§7.2 and add quoting/error-path tests |
| Unquoted keys | v4.0.0 defines the decoder unquoted-key token rule (§7.4); pins strict acceptance of unquoted required-quote values | Needs audit against current key-token scanning | Verify/align unquoted-key acceptance rules |
| Indentation | v4.0.0 makes indentation depth jumps a strict-mode error; over-indented and trailing root lines must no longer be silently discarded | `options.Strict` exists in [ToonDecoder.cs](src/Toon.DotNet/Decode/ToonDecoder.cs) / [ToonScanner.cs](src/Toon.DotNet/Decode/ToonScanner.cs) but depth-jump and over-indent handling need verification against the new rules | Audit strict-mode indentation validation; add negative test cases |
| Empty list items | v4.0.0 allows bare `[]` bracket pairs as empty inner-array list items | Needs audit | Verify decoder accepts this form |
| UTF-8 handling | v4.0.0 requires strict decoders to reject ill-formed UTF-8 byte input | Needs audit of stream/byte-based decode paths ([ToonStream.cs](src/Toon.DotNet/ToonStream.cs)) | Verify byte-level input validation in strict mode |
| Prototype keys | v4.0.0 makes prototype-key handling normative (§15) | Needs audit — .NET's `JsonElement` model differs from JS prototype pollution concerns, but the normative handling (e.g. rejecting/escaping `__proto__`-like keys) should still be verified | Confirm decoder behavior matches §15 intent for this platform |
| Tabular key reordering | v4.0.0 exempts tabular key reordering from the §2 round-trip predicate | Needs audit of round-trip/`Diff` behavior for tabular arrays | Verify round-trip helpers don't over-strictly require key order preservation for tabular rows |
| Token trimming | v4.0.0 restricts token trimming to U+0020 only (promoted to MUST) | Needs audit — current trimming may use `string.Trim()` defaults, which trim a broader whitespace set than U+0020 | Audit trimming calls in the scanner/normalizer for over-broad whitespace handling |
| Malformed headers | v4.0.0 classifies length-less bracket segments as malformed headers | Needs audit of header-parsing error paths | Add negative tests for length-less bracket segments |
| Misplaced scalars | v4.1.0 makes a misplaced scalar line an error in **both** strict and non-strict modes (previously only strict) | Current non-strict path likely tolerates this | This is a behavioral tightening, not additive — decide whether to treat it as a breaking fix and document it as such |

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
Expanded to one concrete, individually testable step per gap-list row, same granularity as [TOON_V3.md](TOON_V3.md)'s fix order. Two open design questions block a clean start on phase 1 — see the end of this section.

### Phase 0 — Groundwork
1. ✅ **Done.** Pin the spec baseline explicitly (`Constants.SpecVersion`, README note, package version) — see [Spec baseline](#spec-baseline) above.

### Phase 1 — Inert API surface (no behavior change yet)
2. Add `EncodeOptions.SpecVersion` (`V3`/`V4` enum), defaulting to `V3` — no behavior wired up yet, just a stable place for later steps to branch on.
3. Add `DecodeOptions.LegacyCompatibility` (or a mirrored `SpecVersion` enum — open question, see below), default `false`/`V4` — also inert initially.

### Phase 2 — Additive/superset grammar (safe, no flag needed)
`\uXXXX` escape handling is already done as a side effect of the v3 work (TOON_V3.md fix-order item 8), so it's dropped from this list. Remaining, roughly dependency-ordered:
4. Bare `[]` as an empty inner-array list item.
5. Full-line `#` comments — scanner-level, gated by the existing (currently unimplemented) `AllowComments` option, not `SpecVersion`.
6. Nested field groups in tabular headers (RFC #46) — encoder + decoder header-grammar change.
7. Keyed tabular form for objects (RFC #57) — builds directly on step 6's header-grammar work.
8. Tabular key-reordering exemption — loosen round-trip/`Diff` comparison so it doesn't over-strictly require key order for tabular rows.
9. Prototype-key handling (§15) — audit and implement normative handling for dangerous key names.
10. Malformed-header classification (length-less bracket segments) — needs the second open design question resolved first (see below).

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

### Open design questions (resolve before starting phase 1 cleanly)
- Step 3: single `LegacyCompatibility` bool, or a `DecodeOptions.SpecVersion` enum mirroring the encoder's? Not yet decided.
- Step 10: is malformed-header classification `Strict`-gated (matches the pattern from the v3 malformed-bracket fix) or unconditional? The spec text doesn't make this unambiguous.