# Build and Test

## Build commands
`powershell
dotnet build ToonFormat.sln -c Debug
dotnet build ToonFormat.sln -c Release
`

## Test commands
`powershell
dotnet test tests/Toon.DotNet.Tests/Toon.DotNet.Tests.csproj -c Debug
dotnet test tests/Toon.DotNet.Tests/Toon.DotNet.Tests.csproj -c Release
`

## Notes
- The test project references the core library and the Excel/CSV integration projects.
- Use the test project as the first validation step for parser and serializer changes.
- Build output and test results may appear under the build and TestResults folders.