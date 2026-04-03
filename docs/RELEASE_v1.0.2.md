# v1.0.2

Hotfix for update manager pointing to wrong repository.

## What's Fixed

- **Update URL migration**: Users who installed v1.0.0 or v1.0.1 had `"gitHubRepo": "autoclick/autoclick"` saved in their local `settings.json`. This caused the update checker to query a non-existent repository, resulting in 404 errors. v1.0.2 adds an automatic migration that detects and corrects this value on startup.

## Upgrade Recommendation

> **All users on v1.0.0 or v1.0.1 should upgrade to v1.0.2.** Auto-update will not function correctly on earlier versions. If auto-update fails, download this release manually from the link below.

## How It Works

On first launch, `SettingsService.Load()` checks if `GitHubRepo` is set to the old `"autoclick/autoclick"` value. If so, it auto-corrects to `"poli0981/autoclick"` and saves the file. This runs once and is transparent to the user.

---

**Full Changelog:** https://github.com/poli0981/autoclick/compare/v1.0.1...v1.0.2
