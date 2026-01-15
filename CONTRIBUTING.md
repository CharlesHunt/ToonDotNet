# Contributing

Thanks for your interest in contributing to ToonFormat for .NET!

- Use .NET10 SDK.
- Run tests before submitting changes.
- Follow existing coding style and enable nullable.
- Keep public APIs documented with XML comments.

Development workflow:
- Fork and create a feature branch.
- Add tests for new behavior.
- Keep commits small and meaningful.
- Update `CHANGELOG.md` when user-facing changes occur.

To build and test locally:
- `dotnet build`
- `dotnet test`

Releasing:
- Update version in `src/ToonFormat/ToonFormat.csproj`.
- Update `CHANGELOG.md`.
- Create a Git tag and publish to NuGet.

