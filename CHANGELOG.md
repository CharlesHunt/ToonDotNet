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
