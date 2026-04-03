# v1.0.3

Critical hotfix for the update system.

## Do NOT use v1.0.0 ~ v1.0.2

Versions 1.0.0, 1.0.1, and 1.0.2 all contain a broken update URL (`autoclick/autoclick` instead of `poli0981/autoclick`). This defect **cannot be fixed through auto-update or reinstall** because the wrong value was either baked into the settings file or shipped in cached binaries due to build issues.

**If you are on any version before 1.0.3, uninstall and download this release manually.**

## What's Fixed

- The GitHub repository URL is now a **compile-time constant** hardcoded in `UpdateService`. It no longer reads from `settings.json` or any runtime configuration.
- The `GitHubRepo` property has been **completely removed** from `AppSettings`. Old `settings.json` files that still contain this key will simply ignore it on deserialization.
- The migration code added in v1.0.2 has been removed (no longer needed).
- Build output was fully cleaned and recompiled to ensure no stale cached binaries are shipped.

## Verification

After launching v1.0.3, the log should show:
```
[INF] Update manager initialized for https://github.com/poli0981/autoclick (installed: True, current: 1.0.3)
```

If you still see `autoclick/autoclick` in the log, you are **not** running v1.0.3. Re-download from this release.

---

**Full Changelog:** https://github.com/poli0981/autoclick/compare/v1.0.2...v1.0.3
