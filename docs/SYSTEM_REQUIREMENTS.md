# System Requirements

**Applies to:** AutoClick 2.0 and later. **Last updated:** October 2026.

## Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| **Windows** | Windows 10 version 2004 (build 19041) | Windows 11, or Windows 10 22H2 |
| **Architecture** | 64-bit (x64) | 64-bit (x64) |
| **Memory** | 4 GB | 8 GB or more, plus what your games need |
| **Disk** | About 170 MB for the app, plus your settings and logs | |
| **Runtime** | Included: .NET 10 ships inside AutoClick (self-contained) | |
| **Rights** | Standard user | Administrator only for games that run as administrator |

AutoClick itself is light: about 160 MB of memory with the window open and close to 0% CPU while idle or clicking at normal intervals. The load you notice comes from the games.

> **Several games at once** need the combined requirements of every game you run, plus the operating system. For example, a game that needs 4 GB and another that needs 6 GB need at least 10 GB together. Watch temperatures and memory use, and run fewer games if the PC throttles or stutters.

## Older Windows versions

AutoClick 2.0 does not run on Windows 7, 8, 8.1 or Windows 10 before version 2004.

- **Windows 10 before 2004** and **Windows 8.1** users can keep using AutoClick **1.3.x** (Windows 10) or **1.1.0** (Windows 8.1, without the dashboard, needs the [Visual C++ 2015 Redistributable](https://www.microsoft.com/en-us/download/details.aspx?id=48145)). These versions no longer receive fixes.
- **Windows 7** has never been supported.

## Game modes

| Game mode | Clicking | Heatmap overlay |
|-----------|----------|-----------------|
| Windowed | Supported | Supported |
| Borderless window | Supported | Supported |
| Fullscreen exclusive | Depends on the game | Usually hidden by the game |

See [HEATMAP_COMPATIBILITY.md](HEATMAP_COMPATIBILITY.md) for the overlay details and [DEV_ENVIRONMENT.md](DEV_ENVIRONMENT.md) for the games tested so far.

## Display scaling and multiple monitors

AutoClick is per-monitor DPI aware. The coordinate picker and the heatmap work on every monitor and at any scaling, and points are stored in the game's own coordinates. Points picked with AutoClick 1.x in a game that is not DPI aware, on a scaled monitor, may be off and should be picked again.

## Tested configurations (2.0)

| Configuration | What was tested |
|---------------|-----------------|
| Developer PC: Windows 11 Pro Insider Preview (Dev Channel) build 29648, Core i7-14700KF, RTX 5080, 32 GB ([details](pc_spec.md)) | Everything, including end-to-end checks against the click target tool |
| GitHub Actions `windows-latest` runner | Build, all tests, and the smoke test of the published app in every theme and language, on every change |
| Windows Sandbox on the developer PC | Before each release: fresh install, update from the previous releases with data kept, and uninstall |

AutoClick 1.x was additionally tested in VirtualBox on Windows 10 22H2 (build 19045: all features) and Windows 8.1 (1.1.0 only).
