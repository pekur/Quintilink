# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade Quintilink\Quintilink.csproj

## Settings

This section contains settings and data used by execution steps.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                              | Current Version | New Version | Description                                          |
|:------------------------------------------|:---------------:|:-----------:|:-----------------------------------------------------|
| Microsoft.Extensions.DependencyInjection  | 9.0.10          | 10.0.0      | Recommended for .NET 10.0                            |
| System.IO.Ports                           | 9.0.10          | 10.0.0      | Recommended for .NET 10.0                            |
| WPF-UI                                    | 4.0.3           | 4.1.0       | Upgrade to latest version                            |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### Quintilink\Quintilink.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0-windows` to `net10.0-windows`

NuGet packages changes:
  - Microsoft.Extensions.DependencyInjection should be updated from `9.0.10` to `10.0.0` (*recommended for .NET 10.0*)
  - System.IO.Ports should be updated from `9.0.10` to `10.0.0` (*recommended for .NET 10.0*)
  - WPF-UI should be updated from `4.0.3` to `4.1.0` (*upgrade to latest version*)
