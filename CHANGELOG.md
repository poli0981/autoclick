# Changelog

All notable changes to AutoClick will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.1.0] - 2026-04-04

### Added
- **Multi-point click sequence**: Add multiple coordinates per game, executed in order each cycle (`#1 → #2 → #3 → [interval] → repeat`). "Add Point" button replaces old "Select Coordinate"; "Clear Points" to reset. Click count now tracks individual clicks, not cycles.
- **Configurable sequence delay**: User-editable "Delay between points" (0-10000ms) input on each game card, visible when >1 point exists.
- **WM_MOUSEMOVE for game dialog compatibility**: InputSimulator now sends mouse-move + delay before clicks, fixing issues where game choice/dialog buttons ignored PostMessage clicks.
- **Japanese, Korean, Chinese (Simplified) translations**: 3 new `.resx` files (~100 keys each). Language selector: EN / Tiếng Việt / 日本語 / 한국어 / 中文. All AI-generated.
- **Session statistics panel**: Live stats bar below toolbar showing Total Clicks, Uptime (hh:mm:ss), Clicks/min (average), Peak c/m (realtime interval-based). Resets on Reset App.
- **Sound notifications**: System sounds for Start (Asterisk), Stop (Hand), Pause/Resume (Question), coordinate pick success (Beep), errors (Exclamation). Toggle in Settings > Advanced > "Sound Notifications". Uses `System.Media.SystemSounds` — no external files.

### Fixed
- Peak clicks/min no longer shows absurd values on first tick (was dividing by near-zero elapsed time)

---

## [1.0.3] - 2026-04-03

### Fixed
- **Critical:** Update manager still pointed to wrong repository (`autoclick/autoclick`) on installed builds. Root cause: the repo URL was stored in `AppSettings.GitHubRepo` and read from `settings.json` at runtime — stale values from older installs persisted even after reinstall. Additionally, builds prior to v1.0.3 may have shipped with old cached binaries due to file-lock issues during compilation.
- Repo URL is now a **compile-time constant** in `UpdateService` (`https://github.com/poli0981/autoclick`). The `GitHubRepo` property has been completely removed from `AppSettings` and `settings.json`. No runtime value can override it.
- Removed the v1.0.2 migration code (no longer necessary since the URL is hardcoded).

### Important

> **Do NOT download v1.0.0, v1.0.1, or v1.0.2.** These versions contain a broken update URL that cannot be fixed through auto-update. Only v1.0.3 and later are guaranteed to have the correct update endpoint. If you are on an older version, please download v1.0.3+ manually from the [Releases page](https://github.com/poli0981/autoclick/releases).

---

## [1.0.2] - 2026-04-03

### Fixed
- **Critical:** Update manager pointed to wrong GitHub repository for users who installed v1.0.0 or v1.0.1. Existing `settings.json` files retained the old `autoclick/autoclick` URL even after the code default was corrected. Added automatic migration in `SettingsService.Load()` to detect and fix stale values on startup.

### Important
> **Users on v1.0.0 or v1.0.1 should upgrade to v1.0.2 or later.** Earlier versions may fail to check for updates due to the incorrect repository URL stored in local settings. v1.0.2 includes an auto-migration that fixes this permanently on first launch.

---

## [1.0.1] - 2026-04-03

### Fixed
- Vietnamese translations now use proper Unicode diacritics across all `.resx` entries
- GitHub repository URL in `AppSettings.GitHubRepo` default corrected (`autoclick/autoclick` -> `poli0981/autoclick`)
- Document links in About view updated from `blob/main` to `blob/master` to match actual branch
- Removed orphaned `FreeRamCommand` / `OnFreeRam` (no UI binding existed)
- Removed unused string resources `MaxRAM` and `FreeRAM` from all locale files

### Changed
- Hardcoded English strings in XAML replaced with localized bindings:
  - "Remove All" -> `Strings.RemoveAll`
  - "Random" button -> `Strings.Random`
  - "Reset" button -> `Strings.ResetStats`
  - "Random Min (s)" -> `Strings.RandomMin`
  - "Random Max (s)" -> `Strings.RandomMax`

### Added
- New string resources: `RemoveAll`, `RandomMin`, `RandomMax`, `ResetStats` (EN + VI)
- README, CHANGELOG, Code of Conduct, Developer Environment documentation
- GitHub Issue templates (Bug Report, Feature Request) and PR template
- Auto-ignore policy for Issues and PRs

---

## [1.0.0] - 2026-04-03

### Added

#### Core
- Multi-game queue with per-game click profiles (Random / Fixed interval modes)
- Background clicking via Win32 `PostMessage` API with anti-detection jitter (+/-2px)
- Coordinate picking: manual crosshair overlay and random generation within game window bounds
- Coordinate validation: bounds checking, duplicate detection, retry mechanism
- Auto-remove games when their process exits (with click count logging)
- Duplicate game detection (prevents adding the same PID twice)

#### UI
- WPF application with MVVM architecture on .NET 8
- Dark / Light theme toggle (defaults to system preference)
- Multi-language support: English and Vietnamese via `.resx` resource files
- Game cards with status badge (localized: Idle / Running / Paused / Stopped) and color coding
- Real-time log panel with toggle visibility
- System tray integration with double-click restore and context menu

#### Settings
- Configurable click mode, intervals, max games in queue
- Global hotkeys: Start All (F8), Stop All (F7), Pause/Resume (F6) with live capture
- Exit behavior setting: Minimize to Tray / Stop All & Exit / Force Exit
- Auto-update toggle
- Settings persisted to `%LocalAppData%\AutoClick\settings.json`
- Export / Import settings as JSON
- "Open Settings File" button
- "Save Settings" button with immediate persistence
- Reset App to factory defaults

#### Update
- Velopack auto-update via GitHub Releases API
- Manual check / download / apply workflow in About tab
- Auto-update on startup (optional, configurable)
- Comprehensive GitHub API error handling: 200, 304, 403 (rate limit), 404 (no release), 429, 5xx, network errors, checksum failures

#### Legal & Documentation
- GPL-3.0 License
- Privacy Policy, Disclaimer, Terms of Service, EULA, Acknowledgements, Security Policy
- Anti-cheat warning banners in About view
- AI-assisted development disclosure

#### Architecture
- 4-project solution: `AutoClick.Core`, `AutoClick.Win32`, `AutoClick.Services`, `AutoClick.UI`
- Dependency Injection via `Microsoft.Extensions.DependencyInjection`
- Serilog structured logging with file sink
- Win32 P/Invoke: `PostMessage`, `EnumWindows`, `RegisterHotKey`, `GetClientRect`, `SetForegroundWindow`
- `ManualResetEventSlim` for pause/resume, `CancellationToken` for stop
- `DispatcherTimer` for UI refresh and process monitoring
