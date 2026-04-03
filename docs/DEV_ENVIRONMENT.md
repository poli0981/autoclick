# Developer Environment

Configuration of the machine used to develop, debug, and test AutoClick. Provided as reference for contributors who want to reproduce the build and test environment.

## Machine Specifications

| Component | Details |
|-----------|---------|
| **OS** | Windows 11 Pro Insider Preview (Dev Channel) |
| **Build** | 26300.8142.ge_prerelease_im.260321-1005 |
| **CPU** | Intel Core i7-14700KF |
| **GPU** | NVIDIA GeForce RTX 5080 (16 GB VRAM) |
| **RAM** | 32 GB DDR5 |
| **Storage** | 1 TB SSD |
| **IDE** | JetBrains Rider 2026.1 |
| **.NET SDK** | 11.0.100-preview.2 (targets `net8.0-windows`) |

## Build Instructions

```bash
git clone https://github.com/poli0981/autoclick.git
cd autoclick
dotnet restore
dotnet build
```

Run the application:
```bash
dotnet run --project src/AutoClick.UI
```

> **Note:** The app requires administrator privileges. When running from IDE, ensure the IDE is launched as Administrator, or right-click the built `.exe` and select "Run as administrator".

## Tested Games

The following games were tested during development. Results are specific to the developer's machine and may vary depending on your system, game version, and anti-cheat configuration.

### Passed

| Game | Store Link | Notes |
|------|------------|-------|
| **Bound Between Desks** | [Steam](https://store.steampowered.com/app/3790060/Bound_Between_Desks/) | PostMessage clicks registered correctly |
| **Myosotis: My Life Is Not Yours to Take** | [Steam](https://store.steampowered.com/app/4220830/Myosotis_My_Life_Is_Not_Yours_to_Take/) | All click modes working |
| **Man in a Suit in a Building in a City** | [Steam](https://store.steampowered.com/app/3292090/Man_in_a_suit_in_a_building_in_a_city/) | Background clicking stable |

### Passed (with caveats)

| Game | Store Link | Anti-Cheat | Notes |
|------|------------|------------|-------|
| **Wuthering Waves** | [Steam](https://store.steampowered.com/app/3513350/Wuthering_Waves/) | ACE (Kernel) | Temporarily passed during testing. **Kernel-level anti-cheat is present** -- use at your own risk. Detection behavior may change with game updates. |

### Failed

| Game | Store Link | Notes |
|------|------------|-------|
| **Mercury Elopement Syndrome** | [Steam](https://store.steampowered.com/app/4417890/Mercury_Elopement_Syndrome/) | PostMessage clicks not recognized by the game. Likely uses DirectInput or Raw Input, which does not respond to `WM_LBUTTONDOWN` messages. |

## Compatibility Notes

- **PostMessage-based clicking** works with games that process standard Windows messages (`WM_LBUTTONDOWN` / `WM_LBUTTONUP`). Games using DirectInput, Raw Input, or custom input pipelines may not respond.
- **Fullscreen exclusive mode** may prevent message delivery. Borderless Windowed or Windowed mode is recommended.
- **Kernel-level anti-cheat** (EasyAntiCheat, BattlEye, Vanguard, ACE) can detect and flag automation tools regardless of the method used. Always check the game's Terms of Service before use.
- **Results may vary** across different hardware configurations, driver versions, and game patches.
