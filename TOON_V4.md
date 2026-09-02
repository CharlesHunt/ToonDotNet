# Toon V4 Assessment

## Summary
Toon V4 should be treated as a compatibility and feature-completion milestone for the existing .NET implementation rather than a greenfield rewrite. The repository already contains a working core serializer, CSV and Excel integration packages, and a substantial xUnit suite, so the work should focus on finishing v4 semantics in the core parser/encoder, keeping the package surface stable, and validating behavior across the current multi-targeting matrix.

## Scope
- Core library: [src/Toon.DotNet/Toon.cs](src/Toon.DotNet/Toon.cs), [src/Toon.DotNet/ToonAsync.cs](src/Toon.DotNet/ToonAsync.cs), [src/Toon.DotNet/ToonStream.cs](src/Toon.DotNet/ToonStream.cs), plus the parser and encoder folders under [src/Toon.DotNet/Decode](src/Toon.DotNet/Decode) and [src/Toon.DotNet/Encode](src/Toon.DotNet/Encode).
- Shared types and constants: [src/Toon.DotNet/Types.cs](src/Toon.DotNet/Types.cs), [src/Toon.DotNet/Constants.cs](src/Toon.DotNet/Constants.cs), and the shared helpers in [src/Toon.DotNet/Shared](src/Toon.DotNet/Shared).
- Integration packages: [src/Toon.DotNet.CSV/ToonCsv.cs](src/Toon.DotNet.CSV/ToonCsv.cs) and [src/Toon.DotNet.Excel/ToonExcel.cs](src/Toon.DotNet.Excel/ToonExcel.cs).
- Test coverage: [tests/Toon.DotNet.Tests](tests/Toon.DotNet.Tests) with focused files such as [tests/Toon.DotNet.Tests/ToonParserTests.cs](tests/Toon.DotNet.Tests/ToonParserTests.cs), [tests/Toon.DotNet.Tests/ToonEncoderTests.cs](tests/Toon.DotNet.Tests/ToonEncoderTests.cs), [tests/Toon.DotNet.Tests/ToonDecoderTests.cs](tests/Toon.DotNet.Tests/ToonDecoderTests.cs), [tests/Toon.DotNet.Tests/StreamTests.cs](tests/Toon.DotNet.Tests/StreamTests.cs), [tests/Toon.DotNet.Tests/ToonCsvTests.cs](tests/Toon.DotNet.Tests/ToonCsvTests.cs), and [tests/Toon.DotNet.Tests/ToonExcelTests.cs](tests/Toon.DotNet.Tests/ToonExcelTests.cs).

## Repo-specific implementation areas
1. Core parser and encoder behavior
   - Confirm that TOON v4 parses nested structures, tabular arrays, delimiters, and edge cases in the current decoder/encoder pipeline.
   - Review the shared normalization and formatting logic in the core library before changing the public entry points.
2. Public API and compatibility
   - Preserve the existing public API in [src/Toon.DotNet/Toon.cs](src/Toon.DotNet/Toon.cs) and avoid introducing breaking changes in the core `Toon` entry points.
   - Keep the current multi-target approach intact: .NET 8/9/10/11 and .NET Standard 2.0 compatibility remain important for downstream consumers.
3. Integration surfaces
   - Ensure the CSV and Excel extensions still round-trip correctly with the core library after any v4 behavior changes.
   - Keep package-level docs and release notes aligned with the changes so the NuGet packages remain predictable.
4. Documentation and examples
   - Update [README.md](README.md), [CHANGELOG.md](CHANGELOG.md), and package-specific docs in [src/Toon.DotNet.CSV/README.md](src/Toon.DotNet.CSV/README.md) and [src/Toon.DotNet.Excel/README.md](src/Toon.DotNet.Excel/README.md) as needed.

## Compatibility strategy
- Keep the implementation compatible with the current target frameworks declared in [src/Toon.DotNet/Toon.DotNet.csproj](src/Toon.DotNet/Toon.DotNet.csproj), [src/Toon.DotNet.CSV/Toon.DotNet.CSV.csproj](src/Toon.DotNet.CSV/Toon.DotNet.CSV.csproj), and [src/Toon.DotNet.Excel/Toon.DotNet.Excel.csproj](src/Toon.DotNet.Excel/Toon.DotNet.Excel.csproj).
- Retain the existing `#if !NETSTANDARD2_0` guards where needed for newer APIs and avoid relying on .NET-only features that would weaken the netstandard2.0 story.
- Prefer additive changes to `EncodeOptions` and `DecodeOptions` over API removal or signature churn, because the current codebase already exposes a broad surface area.
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
- CSV and Excel integrations continue to work with the same round-trip expectations as before.
- The test suite passes with new v4-specific cases added and the documentation reflects the implementation status clearly.
- The release notes state the supported v4 scope and any compatibility caveats for downstream users.

## Recommended implementation order
1. Review the current core parser/encoder flow and identify the exact v4 gaps.
2. Implement the minimum parser/encoder changes needed to satisfy the repo’s current examples and tests.
3. Add regression tests before broadening the feature surface.
4. Update docs and package release notes once the behavior is validated.