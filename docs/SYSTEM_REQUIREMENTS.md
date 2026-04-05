# System Requirements

**Last updated:** April 2026

## Minimum Requirements

| Component | Requirement |
|-----------|-------------|
| **OS** | Windows 8.1 / Windows 8.1 Pro |
| **RAM** | 4 GB |
| **CPU** | Dual-core processor (x64) |
| **GPU** | Required only if running games; no GPU needed for AutoClick itself |
| **Disk** | ~260 MB (self-contained build, includes .NET 8 runtime) |
| **Runtime** | None (self-contained; .NET 8 is bundled in the installer) |

### Windows 8.1 Notes

- **Mandatory prerequisite:** [Visual C++ Redistributable for Visual Studio 2015 (Legacy)](https://www.microsoft.com/en-us/download/details.aspx?id=48145) must be installed before running AutoClick.
- **Dashboard not supported:** The real-time dashboard (LiveChartsCore + SkiaSharp) is not compatible with Windows 8.1. Only **v1.1.0** (without dashboard) is confirmed to work on this OS. If you are a developer, you may remove the Dashboard tab from the source code and build a custom version for Windows 8.1.
- **Core features work:** Multi-game queue, click sequences, profiles, scheduler, pixel color guard, and all other non-dashboard features function correctly on Windows 8.1.

### Windows 7

**Not supported.** AutoClick does not run on Windows 7. The .NET 8 runtime and several Win32 APIs used by AutoClick are not available on Windows 7. Testing confirmed a hard failure on this OS.

---

## Recommended Requirements

| Component | Requirement |
|-----------|-------------|
| **OS** | Windows 10 22H2 (Build 19045) or higher |
| **RAM** | 8 GB or more |
| **CPU** | Quad-core processor (x64) |
| **GPU** | Dedicated GPU if running games |
| **Disk** | ~260 MB + space for game(s) |

> **Multi-game usage:** Running multiple games simultaneously requires significantly more RAM, CPU, and GPU resources than running a single game. Add the minimum requirements of each game you intend to run concurrently. For example, if Game A requires 4 GB RAM and Game B requires 6 GB RAM, you need at least 10 GB RAM plus OS overhead.

---

## Tested Configurations

All tests were performed using the **self-contained (x64)** build unless otherwise noted.

### Physical Machine (Development)

| Component | Details |
|-----------|---------|
| **OS** | Windows 11 Pro Insider Preview (Dev Channel), Build 26300.8142 |
| **CPU** | Intel Core i7-14700KF |
| **GPU** | NVIDIA GeForce RTX 5080 (16 GB VRAM) |
| **RAM** | 32 GB DDR5 |
| **Storage** | 1 TB SSD |
| **Result** | **All features pass** (v1.2.0 full, including dashboard) |

### Virtual Machine — Windows 10

| Component | Details |
|-----------|---------|
| **OS** | Windows 10 22H2, Build 19045.3803 |
| **CPU** | 2 cores (virtualized) |
| **RAM** | 4 GB |
| **VM Host** | Oracle VirtualBox 7.2.6 r172322 (Qt 6.8.0) |
| **Result** | **All features pass** (v1.2.0 full, including dashboard) |

### Virtual Machine — Windows 8.1

| Component | Details |
|-----------|---------|
| **OS** | Windows 8.1 / Windows 8.1 Pro, Build 9600 |
| **CPU** | 2 cores (virtualized) |
| **RAM** | 8 GB |
| **VM Host** | Oracle VirtualBox 7.2.6 r172322 (Qt 6.8.0) |
| **Prerequisite** | [Visual C++ Redistributable for Visual Studio 2015 (Legacy)](https://www.microsoft.com/en-us/download/details.aspx?id=48145) — **mandatory** |
| **Result** | **Partial pass — v1.1.0 only.** Core features work. Dashboard (LiveChartsCore/SkiaSharp) is not compatible with this OS. |

### Virtual Machine — Windows 7

| Component | Details |
|-----------|---------|
| **OS** | Windows 7 |
| **VM Host** | Oracle VirtualBox 7.2.6 r172322 (Qt 6.8.0) |
| **Result** | **Failed.** Application does not launch. .NET 8 runtime is not supported on Windows 7. |

### Testing Tools

| Tool | Version | Purpose |
|------|---------|---------|
| **Oracle VirtualBox** | 7.2.6 r172322 (Qt 6.8.0 on Windows) | Primary VM platform for OS compatibility testing |
| **Windows Sandbox** | (Windows 10/11 built-in) | Quick isolated testing environment |

---

## Compatibility Matrix

| OS | Version | AutoClick v1.2.0 | AutoClick v1.1.0 | Notes |
|----|---------|:-:|:-:|-------|
| Windows 11 | 23H2+ | **Full** | **Full** | Recommended |
| Windows 10 | 22H2 (19045) | **Full** | **Full** | Recommended minimum |
| Windows 10 | Older builds | Likely works | Likely works | Not officially tested |
| Windows 8.1 | Build 9600 | Partial | **Full** | Requires VC++ 2015. No dashboard in v1.2.0 |
| Windows 8 | — | Unknown | Unknown | Not tested |
| Windows 7 | — | **Failed** | **Failed** | Not supported |

**Legend:**
- **Full** — All features work as intended
- **Partial** — Core features work; dashboard/charts unavailable
- **Failed** — Application does not launch

---

## Performance Considerations

### Single Game
- AutoClick itself consumes minimal resources (~30-50 MB RAM, <1% CPU at idle, <2% CPU during active clicking)
- Performance impact depends almost entirely on the game being automated

### Multiple Games
- Each additional game increases total system resource consumption
- **Always check the minimum requirements on the game's store page** (Steam, Epic, etc.) and ensure your system can handle the combined load
- Monitor CPU temperature, GPU temperature, and RAM usage during multi-game operation
- If you observe thermal throttling, excessive fan noise, or sluggish performance, reduce the number of concurrent games
- Consider using **Windowed** or **Borderless Windowed** mode for all games to reduce GPU overhead compared to Fullscreen Exclusive

### Dashboard
- The real-time dashboard (LiveChartsCore + SkiaSharp) adds moderate GPU/CPU usage when the Dashboard tab is open
- Charts update every 2 seconds; closing the Dashboard tab does not stop the timer but hides rendering
- On low-spec systems, avoid keeping the Dashboard tab visible during heavy multi-game operation
