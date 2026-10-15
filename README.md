# AutoClick

<p align="center">
  <strong>Background auto-clicker for Windows games</strong><br/>
  Clicks and key presses sent to game windows without moving your mouse &bull; multi-step sequences &bull; several games at once &bull; 5 languages
</p>

<p align="center">
  <a href="https://github.com/poli0981/autoclick/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/poli0981/autoclick?sort=semver"></a>
  <a href="https://github.com/poli0981/autoclick/actions/workflows/ci.yml"><img alt="CI" src="https://github.com/poli0981/autoclick/actions/workflows/ci.yml/badge.svg?branch=master"></a>
  <a href="https://github.com/poli0981/autoclick/actions/workflows/codeql.yml"><img alt="CodeQL" src="https://github.com/poli0981/autoclick/actions/workflows/codeql.yml/badge.svg?branch=master"></a>
  <a href="https://scorecard.dev/viewer/?uri=github.com/poli0981/autoclick"><img alt="OpenSSF Scorecard" src="https://api.scorecard.dev/projects/github.com/poli0981/autoclick/badge"></a>
  <a href="LICENSE"><img alt="License: GPL-3.0" src="https://img.shields.io/github/license/poli0981/autoclick"></a>
</p>

<p align="center">
  <a href="#features">Features</a> &bull;
  <a href="#installation">Installation</a> &bull;
  <a href="#usage">Usage</a> &bull;
  <a href="#building-from-source">Building</a> &bull;
  <a href="#connect--support">Connect &amp; Support</a> &bull;
  <a href="#legal">Legal</a>
</p>

---

AutoClick sends mouse clicks and key presses to game windows with the Win32 `PostMessage` API. It runs as a separate program, does not modify or inject into games, and leaves your own mouse and keyboard free while it works.

> **Anti-cheat warning:** games with kernel-level anti-cheat (EasyAntiCheat, BattlEye, Vanguard, ...) may detect automation. **Use at your own risk**; the developer is not responsible for account bans. See the [Disclaimer](docs/DISCLAIMER.md) and [EULA](docs/EULA.md).
>
> **Hardware warning:** running several games at once raises CPU, memory and GPU load. Make sure your PC meets each game's requirements. See the [Disclaimer](docs/DISCLAIMER.md).
>
> **AI disclosure:** AutoClick is built with AI assistance (Claude by Anthropic). The in-app translations are AI-generated and may contain mistakes. See [Acknowledgements](docs/ACKNOWLEDGEMENTS.md).

## What's new in 2.0

A new Fluent interface, administrator rights only when a game needs them, keystroke steps that survive saving, a picker that works on every monitor and display scaling, sessions that wait while a game is minimized, and fixes for every known 1.x bug. Settings, profiles and hotkeys from 1.x carry over. Full list: [CHANGELOG](CHANGELOG.md#200---2026-10-15).

## Features

- **Game queue**: add up to 50 game windows (10 by default), each with its own steps and statistics; start, pause, resume and stop them one by one or all together.
- **Step sequences**: left click, double click, right click or a key press per step, run in order with a delay between steps, then repeated after the interval.
- **Timing**: fixed or random interval (1 s to 1 h). *Global* mode uses one timing for every game; *Custom* mode gives each game its own timing and profiles.
- **Coordinate picker**: a crosshair over exactly the game's window area, on any monitor and display scaling. Keys `1`-`4` switch between left, double, right click and key press; `Esc` cancels. Or add a random point.
- **Pixel color guard**: remember the color under each point and only click while it still matches; otherwise skip the step, stop, or wait until it matches.
- **Profiles**: save, load, import and export steps and timing (`.autoclick` files).
- **Sessions**: export or import everything (settings, profiles and the game queue) in one `.autoclick-session` file.
- **Scheduler**: start everything at a time of day and optionally stop at another (overnight windows work).
- **Heatmap overlay**: where the clicks landed, drawn over the game window while the game is in front.
- **Dashboard**: clicks per minute (live), clicked vs. skipped, per-game breakdown and timeline, CSV export.
- **Global hotkeys**: Pause/Resume all (`F6`), Stop all (`F7`), Start all (`F8`), changeable, with conflict detection.
- **Administrator only when needed**: AutoClick tells you when a game runs as administrator and offers to restart elevated.
- **Notifications and tray**: notification-area icon, a notice when a game exits, optional sounds; closing the window can minimize to the tray, stop and exit, or exit.
- **Themes and languages**: System, Light, Dark or High contrast; English, Tiếng Việt, 日本語, 한국어, 中文, switchable without a restart.
- **Updates**: from GitHub Releases through Velopack, checked at startup and every 6 hours, installed when you choose or when you exit.

## Requirements

| | |
|---|---|
| **Windows** | Windows 10 version 2004 (build 19041) or later, or Windows 11 |
| **CPU** | 64-bit (x64) |
| **Runtime** | Included (.NET 10, self-contained): nothing else to install |
| **Rights** | Standard user. Administrator only for games that run as administrator |

Games work best in **windowed or borderless** mode. See [System Requirements](docs/SYSTEM_REQUIREMENTS.md) for tested configurations and the [heatmap compatibility notes](docs/HEATMAP_COMPATIBILITY.md).

## Installation

1. Open the [latest release](https://github.com/poli0981/autoclick/releases/latest).
2. Download **`AutoClick-win-Setup.exe`** (installs for your Windows account, no administrator prompt, and updates itself) or **`AutoClick-win-Portable.zip`** (unzip anywhere).
3. Windows SmartScreen may warn that the app is unrecognized because the files are not code-signed. Choose **More info → Run anyway** if the file came from this repository's Releases page.

To check that a download was built by this repository's release workflow, use the [GitHub CLI](https://cli.github.com/):

```bash
gh attestation verify AutoClick-win-Setup.exe -R poli0981/autoclick
```

**Upgrading from 1.x:** update from inside AutoClick or run the new installer. Settings, profiles and hotkeys are migrated; the 1.x settings file is kept unchanged so 1.x still works if you go back. Because 1.x always ran as administrator, AutoClick 2 keeps doing so for upgraded installs until you turn it off (a notice explains how).

**Uninstalling** removes AutoClick and its data folder `%LocalAppData%\AutoClick`, including saved profiles: export the ones you want to keep first.

## Usage

1. **Add game** and pick the game's window.
2. **Add step** and click the spot in the game (or press `4`, then the key to send). Repeat for more steps; **Random point** adds one automatically.
3. Press **Start** on the game card, or **Start all**.
4. Watch clicks, skips and clicks per minute on the card and on the **Dashboard**; use **Heatmap** to see where clicks land.
5. **Settings** holds timing, the pixel color guard, hotkeys, theme, language, exit behavior and administrator mode; changes apply immediately.

Data lives in `%LocalAppData%\AutoClick`: `settings.v2.json`, `profiles\`, and `logs\` (daily files; the 14 most recent are kept). See the [Privacy Policy](docs/PRIVACY_POLICY.md).

## Building from source

Requirements: Windows 10/11 x64 and the .NET SDK version in [`global.json`](global.json) (10.0.4xx).

```bash
git clone https://github.com/poli0981/autoclick.git
cd autoclick
dotnet tool restore
dotnet build AutoClick.slnx
dotnet test --solution AutoClick.slnx
dotnet run --project src/AutoClick.UI
```

| Project | Contents |
|---|---|
| `src/AutoClick.Core` | Models, settings and file formats (with 1.x migration), scheduling, hotkey parsing, DPI math. No Windows dependencies. |
| `src/AutoClick.Win32` | Win32 interop (CsWin32): input, windows, pixels, DPI scopes, hotkeys, WinEvent hooks, elevation. |
| `src/AutoClick.Services` | Click engine, settings/profile/session stores, scheduler, updates, log buffer. |
| `src/AutoClick.UI` | WPF application (WPF-UI, MVVM), overlays, tray, localization. |
| `tests/` | Core, Services, Win32 and architecture tests (xUnit v3). |
| `tools/AutoClick.ClickTarget` | A test window that logs every click and key it receives. |

See [CONTRIBUTING](CONTRIBUTING.md) for the checks every pull request must pass and [ARCHITECTURE](docs/ARCHITECTURE.md) for the design and its decisions.

## Contributing

Issues and pull requests are welcome; please read [CONTRIBUTING](CONTRIBUTING.md) and the [Code of Conduct](docs/CODE_OF_CONDUCT.md). Issues and pull requests that contain suspicious code or links, are unrelated to the project, or are disrespectful are closed without discussion. Report security problems privately as described in the [Security Policy](docs/SECURITY.md).

## Connect & Support

- **Discord**: [discord.gg/kDM9GMu5vm](https://discord.gg/kDM9GMu5vm)
- **Other profiles** (X, YouTube, Bluesky, Mastodon, Steam, ...): [docs/social.md](docs/social.md) ([tiếng Việt](docs/i18n/vi/social.md))
- **Bugs and ideas**: [Bug report](https://github.com/poli0981/autoclick/issues/new?template=bug_report.yml), [Feature request](https://github.com/poli0981/autoclick/issues/new?template=feature_request.yml), or [Discussions](https://github.com/poli0981/autoclick/discussions)

If AutoClick saves you time, consider supporting its development:

[![GitHub Sponsors](https://img.shields.io/badge/Sponsor-poli0981-EA4AAA?logo=githubsponsors&logoColor=white)](https://github.com/sponsors/poli0981)
[![Ko-fi](https://img.shields.io/badge/Ko--fi-skullmute-FF5E5B?logo=kofi&logoColor=white)](https://ko-fi.com/skullmute)
[![Patreon](https://img.shields.io/badge/Patreon-skullmute-F96854?logo=patreon&logoColor=white)](https://www.patreon.com/skullmute)
[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-skullmute-FFDD00?logo=buymeacoffee&logoColor=black)](https://www.buymeacoffee.com/skullmute)
[![PayPal](https://img.shields.io/badge/PayPal-DungDang212-00457C?logo=paypal&logoColor=white)](https://paypal.me/DungDang212)

The PC used to build and test releases is described in [docs/pc_spec.md](docs/pc_spec.md) ([tiếng Việt](docs/i18n/vi/pc_spec.md)).

## Legal

| Document | Description |
|---|---|
| [LICENSE](LICENSE) | GNU General Public License v3.0 |
| [EULA](docs/EULA.md) | End-user license agreement (the GPL-3.0 prevails if they conflict) |
| [Terms of Service](docs/TERMS_OF_SERVICE.md) | Terms of use |
| [Privacy Policy](docs/PRIVACY_POLICY.md) | What is stored, where, and for how long; nothing is collected |
| [Disclaimer](docs/DISCLAIMER.md) | Anti-cheat and hardware risks, liability |
| [Security Policy](docs/SECURITY.md) | Supported versions and private vulnerability reporting |
| [Acknowledgements](docs/ACKNOWLEDGEMENTS.md) | Third-party software and credits |
| [Code of Conduct](docs/CODE_OF_CONDUCT.md) | Community guidelines |

## License

AutoClick is free software under the [GNU General Public License v3.0](LICENSE). Third-party components keep their own licenses, listed in `THIRD-PARTY-NOTICES.txt` inside every release and in [Acknowledgements](docs/ACKNOWLEDGEMENTS.md).
