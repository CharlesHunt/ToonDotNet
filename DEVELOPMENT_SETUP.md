# Development Setup

## Prerequisites
- .NET SDK 10 or newer is recommended for local development.
- A VS Code installation is sufficient.

## Initial setup
`powershell
dotnet restore
`

## Build
`powershell
dotnet build ToonFormat.sln
`

## Run tests
`powershell
dotnet test tests/Toon.DotNet.Tests/Toon.DotNet.Tests.csproj
`

## Typical workflow
1. Make changes in the core library or an integration project.
2. Run the targeted test project.
3. Update docs or examples when behavior changes.