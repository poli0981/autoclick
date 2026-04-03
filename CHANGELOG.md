# Changelog

All notable changes to AutoClick will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
