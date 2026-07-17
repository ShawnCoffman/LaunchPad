# Support Launchpad

Support Launchpad is a Windows desktop launcher for support and operations workflows.

It is designed for team leads who want to give every team member the same organized, explained set of tools from day one, while still allowing personal additions where team policy permits.

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
- Lets team leads visually create and publish shared team configurations.
- Marks resources as Team or Personal and supports mandatory team-managed content.
- Searches resource names, descriptions, paths, types, and tab names.
- Displays resource descriptions as tooltips and supports custom icons.
- Expands Windows environment variables such as `%USERPROFILE%` and `%ProgramFiles%` in local paths.

## Team Lead Workflow

1. Open **Settings** and enable **Use shared team config**.
2. Select a JSON path in a location the team can read and authorized leads can write, such as a secured network share or synchronized team folder.
3. Select **Manage Team**.
4. Create tabs and add URLs, folders, executables, commands, or PowerShell scripts.
5. Add descriptions that explain when each resource should be used.
6. Mark important tabs and resources as team-managed so members cannot edit or hide them.
7. Configure PowerShell and administrator-launch policy.
8. Select **Publish Team Config**.

Publishing increments the configuration revision, writes the shared file atomically, and preserves the previous version as a `.bak` file.

For zero-touch onboarding, deploy the `SUPPORT_LAUNCHPAD_SHARED_CONFIG` environment variable with the shared JSON path. When present, Launchpad automatically enables and uses that team configuration, allowing an installer, Group Policy, Intune, or another management tool to provision new members without manual setup.

## Team Member Workflow

- Team resources load from the configured shared file.
- Personal tabs and resources remain in the user's roaming profile.
- The search box filters the entire launchpad.
- **Refresh** pulls in the latest published team configuration.
- Team-managed resources are protected while optional resources can be personalized.

## Shared Configuration Security

The shared configuration can launch applications, commands, and scripts. Treat its location as a trusted administrative source:

- Give normal team members read-only access.
- Limit write access to authorized leads or administrators.
- Restrict PowerShell to approved directories where scripts are used.
- Prefer signed and centrally maintained scripts for larger deployments.

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
