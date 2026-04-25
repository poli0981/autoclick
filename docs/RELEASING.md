# Release Process

AutoClick uses [Velopack](https://velopack.io/) for packaging and updates, with GitHub Actions handling build & release automation. This doc describes the steps to cut a new release.

## Prerequisites

- You can push to `master` and create tags on `github.com/poli0981/autoclick`.
- `.github/workflows/release.yml` is present (the workflow we lean on).
- The repo's default `GITHUB_TOKEN` has write access to releases (already granted by `permissions: contents: write` in the workflow).

## Steps

### 1. Bump version in source

Edit the `<Version>`, `<AssemblyVersion>`, `<FileVersion>`, `<InformationalVersion>` (UI only) entries in all four `.csproj` files:

- `src/AutoClick.UI/AutoClick.UI.csproj`
- `src/AutoClick.Services/AutoClick.Services.csproj`
- `src/AutoClick.Win32/AutoClick.Win32.csproj`
- `src/AutoClick.Core/AutoClick.Core.csproj`

All four must match (e.g. `1.2.1` and `1.2.1.0`).

### 2. Update CHANGELOG.md

Add a section at the top following the existing format:

```markdown
## [X.Y.Z] - YYYY-MM-DD

### Added
- ...

### Fixed
- ...
```

The release workflow extracts the body of this section verbatim into the GitHub Release notes.

### 3. Commit and push

```bash
git add -A
git commit -m "chore: bump version to X.Y.Z"
git push origin master
```

### 4. Tag the release

```bash
git tag vX.Y.Z
git push origin vX.Y.Z
```

The push of a `v*` tag triggers `.github/workflows/release.yml`, which will:

1. Resolve the version from the tag.
2. Extract release notes from `CHANGELOG.md`.
3. `dotnet publish` the UI project as self-contained `win-x64`.
4. Run `vpk download github` to fetch prior releases (so deltas can be computed).
5. Run `vpk pack` to produce:
   - `releases.win.json`
   - `AutoClick-X.Y.Z-full.nupkg`
   - `AutoClick-X.Y.Z-delta.nupkg`
   - `AutoClick-win-Portable.zip`
   - `AutoClick-win-Setup.exe`
6. Run `vpk upload github` to attach the artifacts to a GitHub Release named `AutoClick vX.Y.Z`.

### 5. Publish the draft

`vpk upload github` creates the release as a **draft** by default. After the workflow succeeds:

```bash
gh release edit vX.Y.Z --repo poli0981/autoclick --draft=false
```

Or open the release on GitHub and click "Publish release". Until this is done the release is invisible to end users and the auto-updater won't pick it up.

### 6. Manual trigger (alternative)

If you need to re-run a release without re-tagging (e.g. transient CI failure), use **Actions → Release → Run workflow** and enter the version number (e.g. `1.2.1`). The workflow uses `--merge` on upload, so re-runs add to the existing release rather than failing.

## Verifying a release

After the workflow finishes:

1. Open https://github.com/poli0981/autoclick/releases — the new release should be there.
2. Confirm the five Velopack artifacts are attached.
3. Open `releases.win.json` — the new version should appear in the list with valid SHA hashes.
4. Install the previous version locally, open AutoClick → Settings → "Check for updates" — the new version should be detected.

## Code signing

Currently disabled. To enable:

1. Add the `.pfx` certificate as a GitHub repo secret (e.g. `SIGNING_CERT_PFX_BASE64`) and the password as `SIGNING_CERT_PASSWORD`.
2. Decode the cert into a file in the workflow before `vpk pack`.
3. Pass `--signParams "/f cert.pfx /p $env:SIGNING_CERT_PASSWORD /tr http://timestamp.digicert.com /td sha256 /fd sha256"` to `vpk pack`.

Until that's in place, Windows SmartScreen may show a warning on first install for new users. Existing installs auto-update without prompting.
