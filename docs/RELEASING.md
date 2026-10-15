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

The push of a `vX.Y.Z` tag triggers `.github/workflows/release.yml`. Its **build** job (read-only token):

1. Validates the tag format and checks it matches `<Version>` in the projects and that the `vpk` CLI pinned in `.config/dotnet-tools.json` matches the `Velopack` package version.
2. Extracts release notes from `CHANGELOG.md` (fails if the `## [X.Y.Z]` section is missing).
3. `dotnet publish`es the UI project as self-contained `win-x64` and runs the `--smoke-test` of the published exe.
4. Runs `vpk pack` to produce `releases.win.json`, `assets.win.json`, `RELEASES`, `AutoClick-X.Y.Z-full.nupkg`, `AutoClick-win-Portable.zip` and `AutoClick-win-Setup.exe`. Delta packages are currently disabled (`--delta None`) because 1.3.4 changed the .NET runtime.

The **upload draft release** job then runs `vpk upload github` and attaches the artifacts to a **draft** release named `AutoClick vX.Y.Z`. The same packaging step also runs as a dry run in the CI workflow on every pull request, so a broken release-notes section is caught before tagging.

### 5. Verify, then publish the draft

Until it is published the release is invisible to end users and the auto-updater won't pick it up. Check the draft (see "Verifying a release"), then publish it **from your own account**:

```bash
gh release edit vX.Y.Z --repo poli0981/autoclick --draft=false
```

Or open the release on GitHub and click "Publish release". Publishing with a user token raises the `release: published` event, which runs the Discord announcement and the release-discussion workflows (events raised by the workflow's own `GITHUB_TOKEN` would not).

### 6. Manual trigger (alternative)

To rebuild an existing tag without re-tagging (e.g. a transient CI failure), use **Actions → Release → Run workflow** and enter the tag (e.g. `v1.3.4`). The workflow always checks out that tag, and `--merge` adds to the existing draft instead of failing.

## Verifying a release

After the workflow finishes:

1. Open https://github.com/poli0981/autoclick/releases — the new draft should be there.
2. Confirm the Velopack artifacts listed above are attached.
3. Open `releases.win.json` — the new version should appear with valid SHA hashes.
4. Install the previous version (for example in Windows Sandbox), open AutoClick → About → "Check for updates" — the new version should be detected and install cleanly.

## Code signing

Currently disabled. To enable:

1. Add the `.pfx` certificate as a GitHub repo secret (e.g. `SIGNING_CERT_PFX_BASE64`) and the password as `SIGNING_CERT_PASSWORD`.
2. Decode the cert into a file in the workflow before `vpk pack`.
3. Pass `--signParams "/f cert.pfx /p $env:SIGNING_CERT_PASSWORD /tr http://timestamp.digicert.com /td sha256 /fd sha256"` to `vpk pack`.

Until that's in place, Windows SmartScreen may show a warning on first install for new users. Existing installs auto-update without prompting.
