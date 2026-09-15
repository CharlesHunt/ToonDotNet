# Project Status

This repository contains ToonFormat for .NET: a compact serializer library for the TOON format, with support for core encoding/decoding plus optional Excel and CSV integrations.

## Current shape
- Solution: ToonFormat.sln
- Core library: src/Toon.DotNet
- Excel integration: src/Toon.DotNet.Excel
- CSV integration: src/Toon.DotNet.CSV
- Tests: tests/Toon.DotNet.Tests
- Samples: examples/Toon.DotNet.Example

## Snapshot
- The package targets .NET Standard 2.0 and modern .NET targets such as .NET 8, .NET 9, .NET 10, and .NET 11.
- The public surface is centered on the Toon API and its encode/decode helpers.
- The test project is the main regression safety net for behavior changes.