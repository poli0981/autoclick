# AutoClick

<p align="center">
  <strong>Auto-click utility for games on Windows</strong><br/>
  Background clicking via Win32 API &bull; Multi-game queue &bull; Dark/Light theme &bull; EN/VI
</p>

<p align="center">
  <a href="https://github.com/poli0981/autoclick/releases">Releases</a> &bull;
  <a href="#features">Features</a> &bull;
  <a href="#installation">Installation</a> &bull;
  <a href="#usage">Usage</a> &bull;
  <a href="#legal">Legal</a>
</p>

---

## About

AutoClick is a Windows desktop application that automates repetitive mouse clicking in games using Win32 `PostMessage` API. It runs as a separate process and sends click events to game windows in the background -- no game modification required.

> **Anti-Cheat Warning:** Games with kernel-level anti-cheat (EasyAntiCheat, BattlEye, Vanguard, etc.) may detect this tool. **Use at your own risk.** The developer is not responsible for any account bans. See [DISCLAIMER](docs/DISCLAIMER.md) and [EULA](docs/EULA.md).

> **AI Disclosure:** This software was built with AI assistance (Claude by Anthropic). In-app translations are AI-generated and may not be fully accurate. See [ACKNOWLEDGEMENTS](docs/ACKNOWLEDGEMENTS.md).

## Features

- **Multi-game queue** -- add multiple games, each with independent click profiles
- **Click modes** -- Fixed interval or Random interval (configurable min/max)
- **Coordinate picking** -- Manual crosshair picker or random generation within game window
- **Background clicking** -- Uses `PostMessage` with anti-detection jitter (no foreground focus needed)
- **Global hotkeys** -- Start All (F8), Stop All (F7), Pause/Resume (F6), fully customizable
- **Dark / Light theme** -- toggle in settings, defaults to system preference
- **Multi-language** -- English and Vietnamese (expandable via .resx)
- **System tray** -- minimize to tray, configurable exit behavior
- **Auto-update** -- via Velopack + GitHub Releases, optional on startup
- **Settings persistence** -- JSON file at `%LocalAppData%\AutoClick\`, export/import supported
- **Comprehensive logging** -- real-time log panel + file export

## Tech Stack

| Layer | Technology |
|-------|------------|
| UI | WPF (.NET 8), MVVM pattern |
| Click engine | Win32 `PostMessage` via P/Invoke |
| Game detection | `EnumWindows` + process filtering |
| Hotkeys | `RegisterHotKey` global hooks |
| Update | Velopack + GitHub Releases API |
| Logging | Serilog (file + in-memory) |
| DI | Microsoft.Extensions.DependencyInjection |

## Requirements

- Windows 10 / 11
- .NET 8 Desktop Runtime
- Administrator privileges (required for global hotkeys and PostMessage to elevated processes)

## Installation

### From Release (recommended)

1. Go to [Releases](https://github.com/poli0981/autoclick/releases)
2. Download the latest installer
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
2. **Set Coordinate** -- click "Select Coordinate" (crosshair picker) or "Random" (auto-generate)
3. **Start** -- click "Start" on individual games, or "Start All" for all at once
4. **Configure** -- go to Settings tab to adjust intervals, hotkeys, theme, language

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
| [Privacy Policy](docs/PRIVACY_POLICY.md) | No data collection, local-only storage |
| [Disclaimer](docs/DISCLAIMER.md) | Anti-cheat risks, AI disclosure, liability |
| [Terms of Service](docs/TERMS_OF_SERVICE.md) | Usage terms and conditions |
| [EULA](docs/EULA.md) | End-user license agreement |
| [Acknowledgements](docs/ACKNOWLEDGEMENTS.md) | Third-party libraries, AI credits |
| [Security Policy](docs/SECURITY.md) | Vulnerability reporting |
| [Code of Conduct](docs/CODE_OF_CONDUCT.md) | Community guidelines |

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).
