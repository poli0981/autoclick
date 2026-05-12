# PC Specification

Public-facing snapshot of the primary developer workstation. Use this as a reference for "what the project was built and validated on." Specific game/build test results live in [DEV_ENVIRONMENT.md](DEV_ENVIRONMENT.md). The Vietnamese mirror is at [i18n/vi/pc_spec.md](i18n/vi/pc_spec.md).

> Applies to **all** of the developer's active projects, not only AutoClick.

## Developer Machine (Primary)

| Component | Details |
|-----------|---------|
| **OS** | Windows 11 Pro 25H2 Insider Preview (Dev Channel) |
| **Build** | 26300.8376 |
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

Only the toolchains currently in use across active projects are listed. Each project may pin stricter minimums in its own README.

- **Python** — 3.12.x, 3.14.x
- **Node.js** — `>= 25.8.1`
- **Rust** — stable (via `rustup`)
- **Git** — recent; GPG commit signing enabled (`commit.gpgsign=true`)
- **.NET** — 8.x, 9.x, 10.x, 11.x (PREVIEW)

Newer versions will be appended as they appear in shipped projects.

## Related Documents

- [DEV_ENVIRONMENT.md](DEV_ENVIRONMENT.md) — IDE + language toolchains + dev workflow (EN)
- [SYSTEM_REQUIREMENTS.md](SYSTEM_REQUIREMENTS.md) — end-user system requirements
- [i18n/vi/pc_spec.md](i18n/vi/pc_spec.md) — Vietnamese mirror of this document
- [i18n/vi/DEV_ENVIRONMENT.md](i18n/vi/DEV_ENVIRONMENT.md) — Vietnamese mirror of the dev environment doc
