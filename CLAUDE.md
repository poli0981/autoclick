# AutoClick: notes for AI coding agents

Background auto-clicker for Windows games (WPF, .NET 10). Read [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the design and [CONTRIBUTING.md](CONTRIBUTING.md) for the checks.

## Commands

```powershell
dotnet tool restore
dotnet build AutoClick.slnx -c Release -p:ContinuousIntegrationBuild=true   # warnings are errors, like CI
dotnet test --solution AutoClick.slnx -c Release --no-build                 # xUnit v3 on Microsoft.Testing.Platform
dotnet format whitespace AutoClick.slnx --verify-no-changes
dotnet format style AutoClick.slnx --verify-no-changes
dotnet publish src/AutoClick.UI/AutoClick.UI.csproj -c Release -r win-x64 --self-contained true -o artifacts/publish
./eng/Invoke-SmokeTest.ps1 -Exe artifacts/publish/AutoClick.UI.exe        # every page x theme x language
dotnet jb inspectcode AutoClick.slnx --no-build --properties:Configuration=Release --swea --format=Sarif --output=artifacts/inspectcode.sarif
./eng/Test-InspectCode.ps1 -Sarif artifacts/inspectcode.sarif               # unused-code gate
```

## Rules that are easy to break

- **Never change** the Velopack package id `AutoClick`, the exe name `AutoClick.UI.exe`, the channel `win`, or the data folder `%LocalAppData%\AutoClick`: existing installs depend on them. The paths of the legal documents under `docs/` are linked from older releases: do not move them.
- **Version** lives only in `Directory.Build.props`. CI packs a dry-run release with the notes from `CHANGELOG.md`, so a version bump needs a `## [X.Y.Z]` section there.
- **File formats:** profiles keep the 1.x JSON layout (property names, enums as numbers); settings and sessions are versioned and old versions must keep loading. JSON uses reflection-based `System.Text.Json` (source generation overwrote `init` defaults).
- **Layers:** Core has no Windows dependencies; Services and Win32 reference only Core; view models never reference `Views` or `Overlays` (architecture tests).
- **UI text:** only through `Strings*.resx` (`{l:Loc Key}` in XAML, `L.Get`/`L.Format` in code), present in all five languages with identical placeholders; unused keys and hard-coded XAML text fail the architecture tests.
- **No unused code:** InspectCode with solution-wide analysis fails CI on unused members, types, parameters and usings.
- **Win32:** new APIs go in `src/AutoClick.Win32/NativeMethods.txt` (CsWin32). Coordinate work happens inside `DpiScope.ForWindow(game)`.
- **Overlays** are positioned with `WindowGeometry.SetBounds` in physical pixels, never with WPF `Left/Top`.

## Pitfalls seen before

- WPF projects do not get `System.IO` as an implicit using (it is added in the csproj).
- Adding a `<Resource>` item is not picked up by an incremental build: rebuild with `--no-incremental`.
- CommunityToolkit.Mvvm requires the Windows SDK projection at build time; the UI project removes the projection DLL from the output afterwards (`DropWindowsSdkProjection`). Do not set `IncludeWindowsSDKRefFrameworkReferences=false`.
- WPF-UI's `NavigationView` with a `LeftFluent` pane must not have the title bar overlaid on it (the first item would sit under the title bar); the title bar has its own grid row.
- The smoke test uses `%TEMP%\AutoClick-smoke`; a normal run creates `%LocalAppData%\AutoClick`, so clean that up after local manual tests.
- Commits in this repository are GPG-signed; never bypass signing.

## Releasing

Tag `vX.Y.Z` → the release workflow uploads a **draft** (full package, SBOM, attestations) → test the update from the previous releases in Windows Sandbox (`eng/Invoke-UpgradeTest.ps1`) → publish from the maintainer's account (`gh release edit vX.Y.Z --draft=false`). Tags are protected: fix mistakes with a new patch version. Details: [docs/RELEASING.md](docs/RELEASING.md).
