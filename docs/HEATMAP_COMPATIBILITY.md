# Heatmap Overlay Compatibility

The click heatmap overlay (added in v1.3.0, foreground-aware in v1.3.1) renders a transparent, click-through layer above the game's client area, plotting each clicked coordinate as a colored circle on a blue→red gradient sized by relative click frequency.

Compatibility depends on how the game presents its window to the desktop compositor:

- **Windowed** — works reliably. Standard top-level window, overlay sits above via normal z-order.
- **Borderless windowed** (a.k.a. *fullscreen window*) — works reliably. Same compositor path as windowed.
- **Exclusive fullscreen** — depends on the game's rendering pipeline. Games using DWM-friendly DirectX 11/12 swap chains often work; legacy DirectX 9 or older OpenGL exclusive-fullscreen bypass the desktop compositor and the overlay cannot render above them.

**This is a Windows OS-level limitation, not an AutoClick bug.** No overlay technology built on top-level WPF windows can render above an application that has taken exclusive ownership of the screen surface.

## Tested games

Last updated: 2026-04-25 (v1.3.1)

| Game | Engine | Windowed | Borderless | Fullscreen | Notes |
|------|--------|---------:|-----------:|-----------:|-------|
| [The Solitary Existence of a Little Universe](https://store.steampowered.com/app/3719550/) | Ren'Py | ✅ | — | ❌ | |
| [Bound Between Desks](https://store.steampowered.com/app/3790060/) | Ren'Py | ✅ | — | ❌ | |
| [Wuthering Waves](https://store.steampowered.com/app/3513350/) | Unreal Engine 4 | — | ✅ | ✅ | DX12 swap chain composites through DWM |
| [Deadly Heart Gambit](https://store.steampowered.com/app/3202110/) | Unity | ✅ | ✅ | ❌ | |
| [STEINS;GATE](https://store.steampowered.com/app/412830/) | (unknown) | ✅ | — | ✅ | |

**Legend:** ✅ tested + renders correctly · ❌ tested + does not render · — not tested

## Patterns observed so far

- **Ren'Py** and **Unity** in true exclusive fullscreen → overlay does not render. Switch to Windowed or Borderless.
- **Unreal Engine 4 (DirectX 12)** → overlay renders even in fullscreen, because UE4's modern swap chain composites through DWM.
- **Older or niche engines** vary individually — some legacy titles render fine in fullscreen, some don't. Hard to generalize without per-engine fingerprinting.

If your game falls in the "doesn't render in fullscreen" category, the simplest fix is to switch the game to **Borderless windowed** in its display settings. Performance is comparable to true fullscreen on most modern GPUs, and the overlay will work.

## Reporting / contributing

If you've tested AutoClick's heatmap with a game not listed here, please contribute a row to the table:

1. **Open a PR** adding the game to the table above with:
   - Game name + Steam URL (or other storefront link)
   - Engine, if known (check the game's Steam page or PCGamingWiki)
   - Mode(s) tested: Windowed / Borderless / Fullscreen
   - Result per mode: ✅ shows / ❌ doesn't show
   - Notes (DirectX version, anti-cheat, alt-tab quirks, etc.)
2. **Or open an issue** with the same information and we'll add it.

We aim to grow this list over releases so users can check expected behavior before relying on the heatmap feature for a particular title.

## Related limitations

- **Anti-cheat games** (kernel-level: EasyAntiCheat, BattlEye, Vanguard) may flag the overlay or AutoClick itself. The heatmap is purely visual (click-through, no input injection) but a topmost transparent window may still trip heuristics. Use at your own risk — see [DISCLAIMER](DISCLAIMER.md).
- **Multi-monitor**: the overlay correctly tracks the game window across monitors via DPI-aware coordinate translation. No known multi-monitor issues so far.
- **Multiple games simultaneously**: from v1.3.1 the overlay is foreground-aware — only the currently focused game's heatmap is visible at any time, even if multiple heatmaps are toggled on. Alt-Tab between games to swap which heatmap is shown.
