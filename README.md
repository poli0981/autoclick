# AutoClick

<p align="center">
  <strong>Auto-click utility for games on Windows</strong><br/>
  Background clicking via Win32 API &bull; Multi-point sequences &bull; Per-point click types &bull; Real-time dashboard &bull; 5 languages
</p>

<p align="center">
  <a href="https://github.com/poli0981/autoclick/releases">Releases</a> &bull;
  <a href="#features">Features</a> &bull;
  <a href="#installation">Installation</a> &bull;
  <a href="#usage">Usage</a> &bull;
  <a href="#legal">Legal</a>
</p>

---

> **Recommended: v1.2.0+** — Older versions (v1.0.0~v1.0.2) have a broken update URL. Download the latest from the [Releases page](https://github.com/poli0981/autoclick/releases).

## About

AutoClick is a Windows desktop application that automates repetitive mouse clicking in games using Win32 `PostMessage` API. It runs as a separate process and sends click events to game windows in the background -- no game modification required.

> **Anti-Cheat Warning:** Games with kernel-level anti-cheat (EasyAntiCheat, BattlEye, Vanguard, etc.) may detect this tool. **Use at your own risk.** The developer is not responsible for any account bans. See [DISCLAIMER](docs/DISCLAIMER.md) and [EULA](docs/EULA.md).

> **System Requirements Warning:** Running multiple games simultaneously increases CPU, RAM, and GPU usage significantly. **Ensure your system meets the minimum requirements specified by each game's developer.** The developer is not responsible for hardware damage, overheating, system instability, or data loss caused by running multiple games beyond your system's capacity. See [DISCLAIMER](docs/DISCLAIMER.md).

> **AI Disclosure:** This software was built with AI assistance (Claude by Anthropic). In-app translations are AI-generated and may not be fully accurate. See [ACKNOWLEDGEMENTS](docs/ACKNOWLEDGEMENTS.md).

## Features

- **Multi-game queue** -- add multiple games, each with independent click profiles
- **Multi-point click sequences** -- add multiple coordinates per game, executed in order with configurable delay between points (`#1 → #2 → #3 → [interval] → repeat`)
- **Click type per point** -- each point can be Left Click, Double Click, or Right Click independently (press `1`/`2`/`3` in picker)
- **Click modes** -- Fixed interval or Random interval (configurable min/max), Global or Custom (per-game) settings
- **Coordinate picking** -- Manual crosshair picker or random generation within game window
- **Background clicking** -- Uses `PostMessage` with `WM_MOUSEMOVE` + anti-detection jitter, works on game dialogs and choice screens
- **Pixel color guard** -- optionally verify pixel color before clicking; skip or stop on mismatch (configurable tolerance)
- **Game profiles** -- save/load coordinate sets + click settings as named presets; export/import `.autoclick` files
- **Scheduler** -- set start/stop times for automated sessions with live countdown
- **Real-time dashboard** -- CPM line chart, per-game breakdown, success/skip ratio pie, per-game CPM timeline, session summary cards, JSON stats export
- **Session statistics** -- live stats bar: Total Clicks, Skipped, Uptime, Clicks/min, Peak CPM
- **Game exit notifications** -- balloon notification when a game process exits; auto-stop when queue is empty
- **Sound notifications** -- system sounds for Start, Stop, Pause, coordinate pick, errors (toggleable)
- **Global hotkeys** -- Start All (F8), Stop All (F7), Pause/Resume (F6), fully customizable
- **Dark / Light theme** -- toggle in settings, defaults to system preference
- **5 languages** -- English, Tiếng Việt, 日本語, 한국어, 中文
- **System tray** -- minimize to tray, configurable exit behavior
- **Auto-update** -- via Velopack + GitHub Releases, optional on startup
- **Settings persistence** -- JSON file at `%LocalAppData%\AutoClick\`, export/import supported
- **Comprehensive logging** -- real-time log panel + file export

## Tech Stack

| Layer | Technology |
|-------|------------|
| UI | WPF (.NET 8), MVVM pattern |
| Charts | LiveChartsCore + SkiaSharp |
| Click engine | Win32 `PostMessage` via P/Invoke |
| Game detection | `EnumWindows` + process filtering |
| Hotkeys | `RegisterHotKey` global hooks |
| Update | Velopack + GitHub Releases API |
| Logging | Serilog (file + in-memory) |
| DI | Microsoft.Extensions.DependencyInjection |

## Requirements

| | Minimum | Recommended |
|---|---|---|
| **OS** | Windows 8.1 (x64) | Windows 10 22H2+ (x64) |
| **RAM** | 4 GB | 8 GB+ |
| **CPU** | Dual-core (x64) | Quad-core (x64) |
| **Runtime** | None (self-contained) | None (self-contained) |

- No additional runtime required — the release is **self-contained** (includes .NET 8 runtime)
- **Windows 8.1** requires [Visual C++ Redistributable 2015 (Legacy)](https://www.microsoft.com/en-us/download/details.aspx?id=48145) and is limited to v1.1.0 (no dashboard). **Windows 7 is not supported.**
- Administrator privileges required for global hotkeys and PostMessage to elevated processes
- **For multi-game usage:** Ensure your system meets the minimum hardware requirements specified by each game's developer. Running multiple games simultaneously increases CPU, RAM, and GPU load significantly.

See [System Requirements](docs/SYSTEM_REQUIREMENTS.md) for tested configurations, compatibility matrix, and performance notes.

## Installation

### From Release (recommended)

1. Go to [Releases](https://github.com/poli0981/autoclick/releases)
2. Download the latest `.exe` installer (self-contained, no .NET runtime needed)
3. Run and follow the setup wizard

### From Source

```bash
git clone https://github.com/poli0981/autoclick.git
cd autoclick
dotnet build
dotnet run --project src/AutoClick.UI
```

## Usage

1. **Add Game** -- click "Add Game", select a running game window
2. **Add Points** -- click "Add Point" (crosshair picker) or "Random" to build a click sequence. Add multiple points for multi-step automation
3. **Set Delay** -- when >1 point exists, set delay between points (ms) for the sequence timing
4. **Start** -- click "Start" on individual games, or "Start All" for all at once
5. **Monitor** -- watch live stats (Total Clicks, Uptime, Clicks/min, Peak) on the stats bar
6. **Configure** -- go to Settings tab to adjust intervals, hotkeys, theme, language, sounds

### Settings

Settings are saved to `%LocalAppData%\AutoClick\settings.json` and loaded on startup. You can:
- Export / Import settings as JSON
- Open the settings file directly from the app
- Reset to factory defaults

## Project Structure

```
src/
  AutoClick.Core/       # Models, Enums, Interfaces (no dependencies)
  AutoClick.Win32/      # P/Invoke wrappers (NativeMethods, InputSimulator, WindowHelper)
  AutoClick.Services/   # Business logic (ClickEngine, GameDetector, Settings, Hotkey, Log)
  AutoClick.UI/         # WPF app (Views, ViewModels, Themes, Resources, Converters)
docs/                   # Legal documents
```

## Contributing

Contributions are welcome! Please read the guidelines below before submitting.

### Issue & PR Templates

This repo provides built-in templates for [Bug Reports](.github/ISSUE_TEMPLATE/bug_report.yml), [Feature Requests](.github/ISSUE_TEMPLATE/feature_request.yml), and [Pull Requests](.github/PULL_REQUEST_TEMPLATE.md). You may use these templates or write your own, as long as required fields are included.

### Auto-Ignore Policy

Issues and PRs will be automatically ignored if they:
- Contain suspicious code or links (potential malware/data theft)
- Discuss topics unrelated to this project
- Are excessively verbose, vague, or off-topic
- Insult or disrespect any contributor to this repository

Suspicious PRs will be reviewed in a sandboxed VM before merge.

See [Code of Conduct](docs/CODE_OF_CONDUCT.md) for community guidelines.

## Legal

| Document | Description |
|----------|-------------|
| [LICENSE](LICENSE) | GPL-3.0 |
| [System Requirements](docs/SYSTEM_REQUIREMENTS.md) | Tested configs, min/recommended specs, compatibility matrix |
| [Privacy Policy](docs/PRIVACY_POLICY.md) | No data collection, local-only storage |
| [Disclaimer](docs/DISCLAIMER.md) | Anti-cheat risks, system requirements, AI disclosure, liability |
| [Terms of Service](docs/TERMS_OF_SERVICE.md) | Usage terms and conditions |
| [EULA](docs/EULA.md) | End-user license agreement |
| [Acknowledgements](docs/ACKNOWLEDGEMENTS.md) | Third-party libraries, AI credits |
| [Security Policy](docs/SECURITY.md) | Vulnerability reporting |
| [Code of Conduct](docs/CODE_OF_CONDUCT.md) | Community guidelines |

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).
