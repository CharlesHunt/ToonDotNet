# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog and this project adheres to Semantic Versioning.

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
-- Encode **DataTable** - Convert a data table to TOON format. 
