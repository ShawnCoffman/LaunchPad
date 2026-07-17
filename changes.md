# Changes

## 2026-07-17

### Team Management and Onboarding

- Added a visual team configuration editor for tabs, resources, ordering, descriptions, and team-managed policy.
- Added atomic shared-config publishing, revision increments, and a `.bak` safety copy.
- Added Team and Personal resource indicators, search, refresh, description tooltips, and icon rendering.
- Added environment-variable expansion for portable shared paths.
- Added explicit PowerShell directory restriction semantics, including deny-all behavior when team and personal allowlists do not overlap.
- Prevented personal configuration from hiding mandatory read-only team tabs and resources.
- Filtered read-only destination tabs from personal resource editing.
- Expanded regression coverage from 21 to 26 tests.

## 2026-06-02

### Repository Cleanup

- Removed generated .NET build folders from all projects:
  - `bin/`
  - `obj/`
- Removed local/user-specific files that should not go to GitHub:
  - `SupportLaunchpad.WinForms.csproj.user`
  - empty `SupportLaunchpad.slnx`
- Removed early planning notes from the publishable repo:
  - `BuildPlan.txt`
  - `OfflineKit-V2.txt`
- Added `.gitignore` for .NET build output, IDE files, test results, NuGet artifacts, and common Windows junk files.
- Confirmed the source projects target .NET 10 only.

### Documentation

- Added `README.md` with:
  - project overview
  - project layout
  - local config/log file locations
  - restore/build/test/run commands
  - notes about keeping business logic in `SupportLaunchpad.Core`

### Git Setup

- Initialized a local git repository.
- Created the initial commit on `main`.
- Added GitHub remote:
  - `https://github.com/ShawnCoffman/LaunchPad.git`
- Push was blocked by GitHub authentication in the current environment.

### Review Fixes

- Made read-only shared buttons view-only in the WinForms edit menu.
- Prevented read-only shared buttons from being customized, hidden, moved, or reordered through core edit logic.
- Prevented tab rename/reorder behavior from copying shared buttons into personal user config.
- Added tab name validation for add/rename workflows.
- Added unique tab ID generation when duplicate tab names are used.
- Made invalid launchpad config JSON fall back safely instead of crashing startup.
- Made invalid app settings JSON fall back safely to default settings.
- Made `FallbackToLocalOnly = false` produce an empty effective launchpad when shared config is unavailable, instead of silently running local-only mode.

### Tests

- Added regression tests for:
  - missing shared config with local fallback disabled
  - invalid user config JSON fallback
  - duplicate tab ID generation
  - shared button protection during rename/reorder flows
- Verified:

```powershell
dotnet test
```

Result:

```text
Passed: 16/16
```

```powershell
dotnet build .\SupportLaunchpad.sln --no-restore
```

Result:

```text
Build succeeded, 0 warnings, 0 errors
```
