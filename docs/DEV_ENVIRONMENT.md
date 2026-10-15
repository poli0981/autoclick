# Developer Environment

How AutoClick is built and tested, for contributors who want to reproduce the environment. The machine itself is described in [pc_spec.md](pc_spec.md). The Vietnamese mirror is at [i18n/vi/DEV_ENVIRONMENT.md](i18n/vi/DEV_ENVIRONMENT.md).

## Toolchain

| Tool | Version | Notes |
|------|---------|-------|
| **.NET SDK** | 10.0.4xx | Pinned in [`global.json`](../global.json) (`rollForward: latestFeature`) |
| **Target frameworks** | `net10.0` (Core), `net10.0-windows10.0.19041.0` (everything else) | |
| **Local tools** | [`.config/dotnet-tools.json`](../.config/dotnet-tools.json) | `vpk` (Velopack), ReSharper InspectCode, nuget-license, sbom-tool, ReportGenerator |
| **IDE** | JetBrains Rider 2026.x or Visual Studio Code | Any editor that honors `.editorconfig` |

## Build and test

```bash
git clone https://github.com/poli0981/autoclick.git
cd autoclick
dotnet tool restore
dotnet build AutoClick.slnx
dotnet test --solution AutoClick.slnx
dotnet run --project src/AutoClick.UI
```

AutoClick runs as a normal user; administrator rights are only needed to click games that themselves run as administrator.

The checks CI runs on every pull request, and how to run them locally, are listed in [CONTRIBUTING.md](../CONTRIBUTING.md).

## Test tools

| Tool | Purpose |
|------|---------|
| **`tools/AutoClick.ClickTarget`** | A window that logs every mouse and keyboard message it receives (client coordinates, virtual-key and scan codes). `--dpi unaware\|system\|pmv2` changes its DPI awareness to test coordinate mapping, `--log <file>` writes the log to a file. |
| **`AutoClick.UI.exe --smoke-test`** | Opens every page in every theme and language, the dialogs and a heatmap overlay, and fails on any binding error. Uses `%TEMP%\AutoClick-smoke` instead of the real data folder. |
| **Windows Sandbox** | Install and update tests from an older release (for example 1.3.3 → 2.0.0), without touching the development machine. |

## Tested Games

Results with AutoClick 1.x on the developer's machine; they can differ with your system, game version and anti-cheat configuration.

### Passed

| Game | Store Link | Notes |
|------|------------|-------|
| **Bound Between Desks** | [Steam](https://store.steampowered.com/app/3790060/Bound_Between_Desks/) | PostMessage clicks registered correctly |
| **Myosotis: My Life Is Not Yours to Take** | [Steam](https://store.steampowered.com/app/4220830/Myosotis_My_Life_Is_Not_Yours_to_Take/) | All click modes working |
| **Man in a Suit in a Building in a City** | [Steam](https://store.steampowered.com/app/3292090/Man_in_a_suit_in_a_building_in_a_city/) | Background clicking stable |

### Passed (with caveats)

| Game | Store Link | Anti-Cheat | Notes |
|------|------------|------------|-------|
| **Wuthering Waves** | [Steam](https://store.steampowered.com/app/3513350/Wuthering_Waves/) | ACE (Kernel) | Temporarily passed during testing. **Kernel-level anti-cheat is present**: use at your own risk. Detection behavior may change with game updates. |

### Failed

| Game | Store Link | Notes |
|------|------------|-------|
| **Mercury Elopement Syndrome** | [Steam](https://store.steampowered.com/app/4417890/Mercury_Elopement_Syndrome/) | PostMessage clicks not recognized by the game. Likely uses DirectInput or Raw Input, which does not respond to `WM_LBUTTONDOWN` messages. |

## Compatibility Notes

- **PostMessage-based clicking** works with games that process standard window messages (`WM_LBUTTONDOWN` / `WM_LBUTTONUP`, `WM_KEYDOWN` / `WM_KEYUP`). Games that read DirectInput, Raw Input or their own input pipeline may not respond.
- **Games that run as administrator** only accept input from a program with the same rights; AutoClick detects this and offers to restart as administrator.
- **Fullscreen exclusive mode** may prevent message delivery and overlays. Windowed or borderless mode is recommended.
- **Kernel-level anti-cheat** (EasyAntiCheat, BattlEye, Vanguard, ACE) can detect and flag automation tools regardless of the method used. Check the game's terms of service before use.
