# Heatmap Overlay Compatibility

The click heatmap draws a transparent layer over the game's window area, with one circle per clicked point: blue for rarely clicked, red for the most clicked. The layer cannot be clicked (input passes through to the game), never takes focus and does not appear in Alt+Tab.

How the overlay behaves (AutoClick 2.0):

- It sits exactly over the game's client area, on any monitor and at any display scaling, and follows the game when it moves or is resized.
- It is visible only while the game, or one of the game's own dialogs, is the active window. With several heatmaps open, only the one for the game in front shows.
- It hides while the game is minimized and closes by itself when the game exits or is removed from the queue.
- It redraws only when there are new clicks or the window moved, so it adds almost no load.

## Game display modes

Whether an overlay can appear above a game depends on how the game presents its window:

- **Windowed**: works.
- **Borderless window** (also called *fullscreen windowed*): works.
- **Exclusive fullscreen**: depends on the game. Games whose DirectX 11/12 swap chain goes through the desktop compositor usually work; older DirectX 9 or OpenGL exclusive fullscreen bypasses the compositor and nothing can be drawn above it.

This is a Windows limitation, not an AutoClick bug: no overlay window can draw above a program that owns the screen exclusively. If the heatmap does not show, switch the game to **borderless window**; performance is about the same on modern GPUs.

## Tested games

Tested with AutoClick 1.3.1 (2026-04-25). 2.0 uses the same kind of overlay window, so the display-mode results still apply.

| Game | Engine | Windowed | Borderless | Fullscreen | Notes |
|------|--------|---------:|-----------:|-----------:|-------|
| [The Solitary Existence of a Little Universe](https://store.steampowered.com/app/3719550/) | Ren'Py | ✅ | — | ❌ | |
| [Bound Between Desks](https://store.steampowered.com/app/3790060/) | Ren'Py | ✅ | — | ❌ | |
| [Wuthering Waves](https://store.steampowered.com/app/3513350/) | Unreal Engine 4 | — | ✅ | ✅ | DX12 swap chain composites through DWM |
| [Deadly Heart Gambit](https://store.steampowered.com/app/3202110/) | Unity | ✅ | ✅ | ❌ | |
| [STEINS;GATE](https://store.steampowered.com/app/412830/) | (unknown) | ✅ | — | ✅ | |

**Legend:** ✅ tested and shows · ❌ tested and does not show · — not tested

Patterns so far: Ren'Py and Unity in exclusive fullscreen hide the overlay; Unreal Engine 4 with DirectX 12 shows it even in fullscreen; older or niche engines vary.

## Display scaling and multiple monitors

1.x placed the heatmap using the scaling of the monitor the overlay was on rather than the game's, so it could be offset on setups with different scaling per monitor. 2.0 measures the game window in physical pixels and converts with the game's own DPI.

Click points are stored in the game's own coordinates. Points picked with 1.x in a game that is **not DPI aware**, on a **scaled** monitor, may land slightly off: pick them again in 2.0.

## Reporting

Tested a game that is not listed? Open a pull request adding a row (game and store link, engine if known, the modes tried and the result for each, and notes such as the DirectX version or anti-cheat), or open an issue with the same information.

## Related limitations

- **Anti-cheat** (kernel-level: EasyAntiCheat, BattlEye, Vanguard, ACE) may flag the overlay or AutoClick itself. The heatmap only draws (it never sends input), but a top-most transparent window can still trip heuristics. Use at your own risk; see the [Disclaimer](DISCLAIMER.md).
