# Architecture

The solution is structured around a small core library plus a few focused integrations.

## Components
- Core library: handles TOON parsing, serialization, validation, and file/stream helpers.
- Excel integration: adds spreadsheet-oriented helpers on top of the core API.
- CSV integration: adds CSV-oriented helpers for data interchange.
- Tests: exercise parsing, round-tripping, and compatibility scenarios.
- Examples: show how consumers can use the library in a simple app.

## Design notes
- The core package is intentionally small and dependency-light.
- The public API is designed to be easy to consume from both application code and tooling.
- Extensions live in separate projects so the core package stays focused on the format itself.