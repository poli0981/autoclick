# PC Specification

Public-facing snapshot of the primary developer workstation: what the project is built and tested on. Game and test results live in [DEV_ENVIRONMENT.md](DEV_ENVIRONMENT.md). The Vietnamese mirror is at [i18n/vi/pc_spec.md](i18n/vi/pc_spec.md).

> Applies to **all** of the developer's active projects, not only AutoClick.

## Developer Machine (Primary)

| Component | Details |
|-----------|---------|
| **OS** | Windows 11 Pro Insider Preview (Dev Channel) |
| **Build** | 29648.1000 |
| **CPU** | Intel Core i7-14700KF |
| **GPU** | NVIDIA GeForce RTX 5080 (16 GB VRAM) |
| **RAM** | 32 GB DDR5 |
| **Storage** | 1 TB SSD |
| **IDE** | JetBrains IDEs (paid lineup) 2026.x + Visual Studio Code |

## Mobile Test Devices

Used for cross-browser web verification:

- iPhone 14 Pro — iOS 26.x — Chrome, Brave
- iPhone 13 Pro Max — iOS 26.x — Chrome, Brave

## Toolchain Versions

Only the toolchains currently installed for active projects are listed. Each project may pin stricter versions (AutoClick pins its .NET SDK in [`global.json`](../global.json)).

- **.NET** — SDK 10.0.4xx; runtimes 8.0 and 10.0
- **Python** — 3.14.x
- **Node.js** — 24.x
- **Rust** — stable (via `rustup`)
- **Git** — 2.56; GPG commit signing enabled (`commit.gpgsign=true`)

Newer versions are added as they appear in shipped projects.

## Related Documents

- [DEV_ENVIRONMENT.md](DEV_ENVIRONMENT.md) — build, test tools and tested games
- [SYSTEM_REQUIREMENTS.md](SYSTEM_REQUIREMENTS.md) — end-user system requirements
- [i18n/vi/pc_spec.md](i18n/vi/pc_spec.md) — Vietnamese mirror of this document
- [i18n/vi/DEV_ENVIRONMENT.md](i18n/vi/DEV_ENVIRONMENT.md) — Vietnamese mirror of the development environment document
