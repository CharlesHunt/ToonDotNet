# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade src\ToonFormat\ToonFormat.csproj
4. Upgrade examples\ToonFormat.Example\ToonFormat.Example.csproj
5. Upgrade tests\ToonFormat.Tests\ToonFormat.Tests.csproj


## Settings

This section contains settings and data used by execution steps.

### Excluded projects

Table below contains projects that do belong to the dependency graph for selected projects and should not be included in the upgrade.

| Project name                                   | Description                 |
|:-----------------------------------------------|:---------------------------:|


### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name           | Current Version | New Version | Description                           |
|:-----------------------|:---------------:|:-----------:|:--------------------------------------|
| System.Text.Json       |   9.0.10        |  10.0.0     | Replace System.Text.Json 9.0.10 with 10.0.0 (recommended by analysis)


### Project upgrade details

#### src\ToonFormat\ToonFormat.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - `System.Text.Json` should be updated from `9.0.10` to `10.0.0` (recommended replacement).

Other changes:
  - Review code for any API breaking changes introduced by .NET 10 and System.Text.Json 10.0.0.


#### examples\ToonFormat.Example\ToonFormat.Example.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

Other changes:
  - Ensure referenced project `..\..\src\ToonFormat\ToonFormat.csproj` is upgraded first and builds successfully.


#### tests\ToonFormat.Tests\ToonFormat.Tests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

Other changes:
  - Update test project references if needed after main project upgrades.

