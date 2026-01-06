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