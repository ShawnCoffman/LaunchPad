# Support Launchpad

Support Launchpad is a Windows desktop launcher for support and operations workflows.

It uses a WinForms UI on top of a reusable core library so the business logic can stay separate from the desktop shell. The long-term idea is that the UI can evolve later without rewriting config, validation, or launch behavior.

## Project Layout

- `SupportLaunchpad.Core`
  Core models and services for config loading, merging, validation, logging, and launch behavior.
- `SupportLaunchpad.WinForms`
  Windows desktop UI.
- `SupportLaunchpad.Tests`
  Unit tests for the core behavior.

## Tech Stack

- .NET 10
- C#
- WinForms
- xUnit

## What The App Does

- Loads a personal launchpad config from the current user's roaming profile.
- Optionally merges in a shared/team config path from app settings.
- Renders tabs and launch buttons in the WinForms UI.
- Supports launching folders, executables, URLs, PowerShell scripts, and command-line commands.
- Logs launch attempts to a local log file.

## Local File Locations

- User config:
  `%AppData%\SupportLaunchpad\launchpad.user.json`
- App settings:
  `%AppData%\SupportLaunchpad\appsettings.json`
- Log file:
  `%LocalAppData%\SupportLaunchpad\Logs\launchpad.log`

## Commands

Run these from the repo root:

### Restore packages

```powershell
dotnet restore .\SupportLaunchpad.sln
```

If you want to force the local NuGet config:

```powershell
dotnet restore .\SupportLaunchpad.sln --configfile .\NuGet.Config
```

### Build the full solution

```powershell
dotnet build .\SupportLaunchpad.sln --no-restore
```

### Build the WinForms app only

```powershell
dotnet build .\SupportLaunchpad.WinForms\SupportLaunchpad.WinForms.csproj --no-restore
```

### Run the WinForms app

```powershell
dotnet run --project .\SupportLaunchpad.WinForms\SupportLaunchpad.WinForms.csproj
```

### Run tests

```powershell
dotnet test .\SupportLaunchpad.sln --no-build
```

If you have not built yet, use:

```powershell
dotnet test .\SupportLaunchpad.sln
```

## Typical Workflow

```powershell
dotnet restore .\SupportLaunchpad.sln --configfile .\NuGet.Config
dotnet build .\SupportLaunchpad.sln --no-restore
dotnet test .\SupportLaunchpad.sln --no-build
dotnet run --project .\SupportLaunchpad.WinForms\SupportLaunchpad.WinForms.csproj
```

## Notes

- This app is Windows-focused because the UI project is WinForms.
- Keep business logic in `SupportLaunchpad.Core`, not in WinForms event handlers.
- The repo is already trimmed to `.NET 10` only.
