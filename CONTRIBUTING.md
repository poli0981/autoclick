# Contributing to AutoClick

Thanks for helping! Bug reports, translations and code are all welcome. Please follow the [Code of Conduct](docs/CODE_OF_CONDUCT.md); report security problems privately as described in the [Security Policy](docs/SECURITY.md).

## Setting up

- Windows 10 version 2004 or later, or Windows 11 (x64)
- The .NET SDK version in [`global.json`](global.json) (10.0.4xx)
- Then:

  ```bash
  dotnet tool restore
  dotnet build AutoClick.slnx
  dotnet test --solution AutoClick.slnx
  ```

The design is described in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md); the test tools (click target window, smoke test, Windows Sandbox) in [docs/DEV_ENVIRONMENT.md](docs/DEV_ENVIRONMENT.md).

## Pull requests

`master` is protected: changes arrive through pull requests, and these checks must pass before merging.

| Check | What it verifies | Run it locally |
|-------|------------------|----------------|
| **build-test** | Formatting, a build with warnings as errors, all tests with coverage minimums (Core 90%, Services 70%), the published app's smoke test in every theme and language, and a Velopack packaging dry run | see below |
| **dead-code** | No unused code (ReSharper InspectCode with solution-wide analysis) and no XAML bindings to missing properties | see below |
| **licenses** | Every shipped package has a GPL-3.0-compatible license ([`eng/licenses`](eng/licenses)) | `./eng/New-ThirdPartyNotices.ps1 -OutFile artifacts/THIRD-PARTY-NOTICES.txt` |
| **vuln-audit** | No known vulnerability in direct or transitive packages | `./eng/Test-VulnerablePackages.ps1` |
| **dependency-review** | The pull request adds no vulnerable package and no disallowed license (build and test tools that are never shipped are exempt; the list is in the workflow) | GitHub only |
| **analyze (csharp)** | CodeQL security analysis | GitHub only |

SonarQube Cloud also analyzes every pull request from this repository (the **sonarcloud** check, not required): issues, security hotspots and test coverage of the changed code. Pull requests from forks and Dependabot skip it because they cannot read the `SONAR_TOKEN` secret.

Local equivalents of build-test and dead-code (PowerShell):

```powershell
dotnet format whitespace AutoClick.slnx --verify-no-changes
dotnet format style AutoClick.slnx --verify-no-changes
dotnet build AutoClick.slnx -c Release -p:ContinuousIntegrationBuild=true
dotnet test --solution AutoClick.slnx -c Release --no-build

dotnet publish src/AutoClick.UI/AutoClick.UI.csproj -c Release -r win-x64 --self-contained true -o artifacts/publish
./eng/Invoke-SmokeTest.ps1 -Exe artifacts/publish/AutoClick.UI.exe

dotnet jb inspectcode AutoClick.slnx --no-build --properties:Configuration=Release --swea --format=Sarif --output=artifacts/inspectcode.sarif
./eng/Test-InspectCode.ps1 -Sarif artifacts/inspectcode.sarif
```

Please also:

- Describe what changed and why, and how you tested it (a game, or `tools/AutoClick.ClickTarget`).
- Add user-visible changes under `[Unreleased]` in [`CHANGELOG.md`](CHANGELOG.md).
- Use [Conventional Commits](https://www.conventionalcommits.org/) style messages (`fix:`, `feat:`, `docs:`, `ci:`, `chore:`).

## Code guidelines

- **Layers:** `AutoClick.Core` has no Windows dependencies; `AutoClick.Win32` and `AutoClick.Services` depend only on Core; the UI uses all three. View models never reference views or overlays. Architecture tests enforce this.
- **Style:** `.editorconfig` and the .NET analyzers (`latest-recommended`) are enforced; CI treats warnings as errors.
- **Models** are immutable records with a `Normalize()` that clamps every value read from disk or from the user.
- **File formats are a contract:** profiles keep the 1.x JSON layout (property names, enums as numbers) so 1.x can still read them; settings and sessions are versioned (`schemaVersion`) and older versions must keep loading. Add tests with fixture files under `tests/Fixtures` for any change.
- **Win32:** add new APIs to `src/AutoClick.Win32/NativeMethods.txt` (CsWin32). Anything that converts game coordinates runs inside a `DpiScope` for the game window.
- **No unused code:** the dead-code check fails on it. For a deliberate exception, write `// ReSharper disable once <rule>` with a reason.
- **SonarQube Cloud findings** are fixed, or, when the code is right as it is, suppressed with `[SuppressMessage("Category", "Sxxxx:Title", Justification = "why")]`.

## Translations

All UI text lives in `src/AutoClick.UI/Resources/Strings.resx` (English) and `Strings.vi/ja/ko/zh.resx`. Keys are `Group_Name` (for example `Queue_AddGame`).

- Use text in XAML with `{l:Loc Key}` and in code with `L.Get("Key")` or `L.Format("Key", args)`.
- Every key must exist in all five files with the same `{0}` placeholders, and every key must be used: architecture tests check both, and also reject hard-coded text in XAML.
- The current translations are AI-generated; corrections by native speakers are very welcome.

## Dependencies

Package versions are central in [`Directory.Packages.props`](Directory.Packages.props) and build tools in [`.config/dotnet-tools.json`](.config/dotnet-tools.json). Dependabot proposes updates weekly; patch and minor updates merge automatically once CI is green. A new license must be compatible with the GPL-3.0 and added to `eng/licenses/allowed-licenses.json` (used by both license checks).

## Releasing

Maintainers follow [docs/RELEASING.md](docs/RELEASING.md).
