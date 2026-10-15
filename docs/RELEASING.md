# Release Process

AutoClick is packaged and updated with [Velopack](https://velopack.io/); GitHub Actions builds every release. A release is uploaded as a **draft**, verified, and only then published.

## Prerequisites

- You can merge to `master` (through a pull request: the branch is protected) and push tags.
- Tags matching `v*` are protected by a ruleset: they cannot be moved or deleted. A mistake after tagging is fixed with a new patch version, never by re-tagging.

## 1. Prepare the version (pull request)

1. Set the version once in [`Directory.Build.props`](../Directory.Build.props) (`<Version>X.Y.Z</Version>`); every project and the file/assembly versions derive from it.
2. Add a `## [X.Y.Z] - YYYY-MM-DD` section to [`CHANGELOG.md`](../CHANGELOG.md) (move the items from `[Unreleased]`) and add its compare link at the bottom. The section becomes the release notes shown on GitHub, in the in-app update notice and on Discord; CI fails if it is missing.
3. Open a pull request. CI runs the full release packaging as a dry run with the real notes, so problems show up before tagging.
4. Merge when every required check is green.

## 2. Tag

```bash
git switch master
git pull
git tag -s vX.Y.Z -m "AutoClick X.Y.Z"
git push origin vX.Y.Z
```

The tag starts [`release.yml`](../.github/workflows/release.yml). Its **build** job (read-only token plus attestation permissions):

1. Checks that the tag is `vX.Y.Z`, matches the project version, and that the `vpk` CLI pinned in `.config/dotnet-tools.json` matches the Velopack library.
2. Extracts the release notes from `CHANGELOG.md`.
3. Publishes the app (self-contained `win-x64`), writes `THIRD-PARTY-NOTICES.txt` into it (failing on any license outside the policy) and runs the `--smoke-test` of the published exe.
4. Packs with `vpk pack` (full package only): `AutoClick-win-Setup.exe`, `AutoClick-win-Portable.zip`, `AutoClick-X.Y.Z-full.nupkg`, and the update feed `releases.win.json` plus `RELEASES` (read by older clients).
5. Generates an SPDX SBOM and creates build provenance and SBOM attestations for the installer, the portable zip and the package.

The **upload draft release** job then uploads these files (plus `AutoClick-X.Y.Z.spdx.json`) to a draft release named `AutoClick vX.Y.Z`.

To rebuild an existing tag after a transient failure, use **Actions → Release → Run workflow** with the tag; it builds that tag and merges into the existing draft.

## 3. Verify the draft

Until it is published, the release is invisible to users and to the updater.

1. The draft lists the files above, and `releases.win.json` contains the new version.
2. Verify the attestation of the downloaded installer:

   ```bash
   gh attestation verify AutoClick-win-Setup.exe -R poli0981/autoclick
   ```

3. Test the update in **Windows Sandbox** (nothing is installed on your machine), from the previous release and from older ones users may still have. In PowerShell 7:

   ```powershell
   gh release download v1.3.4 -p AutoClick-win-Setup.exe -D artifacts/upgrade-test/old
   gh release download vX.Y.Z -p 'AutoClick-*-full.nupkg' -p AutoClick-win-Setup.exe -D artifacts/upgrade-test/new
   ./eng/Invoke-UpgradeTest.ps1 -OldSetup artifacts/upgrade-test/old/AutoClick-win-Setup.exe -NewDir artifacts/upgrade-test/new
   ```

   The script installs the old release, adds 1.x settings and a profile, updates with the installed `Update.exe` (as the in-app updater does), starts the new version once (data migration) and runs its smoke test, then uninstalls and checks a fresh install. It prints PASS or FAIL for each check and keeps the logs in `artifacts/upgrade-test/runs`.

## 4. Publish

Publish from **your own account** (events raised by the workflow token would not start other workflows):

```bash
gh release edit vX.Y.Z --repo poli0981/autoclick --draft=false --latest
```

Publishing raises `release: published`, which posts the Discord announcement and creates the release discussion. Check both, and that **About → Check for updates** in an older install now finds the release.

## Runtime security updates

AutoClick ships its own .NET runtime. When Microsoft releases a .NET 10 servicing update with security fixes, Dependabot proposes the new SDK in `global.json`; merge it and ship a patch release so installed copies get the fixed runtime.

## Code signing

Releases are not code-signed (Windows SmartScreen may warn new users on first install; updates of existing installs are not affected). Build provenance attestations let anyone check that a file was built by this repository's workflow. Free signing for open-source projects is available from [SignPath Foundation](https://signpath.org/); `vpk pack` accepts `--signParams` or `--azureTrustedSignFile` once a certificate is available.
