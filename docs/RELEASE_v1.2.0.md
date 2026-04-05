# v1.2.0

Major feature update with click type per point, pixel color guard, game profiles, scheduler, real-time dashboard, and comprehensive quality-of-life improvements.

## New Features

### Click Type Per Point
- Each coordinate in a sequence can independently be **Left Click**, **Double Click**, or **Right Click**
- Press `1`/`2`/`3` in the coordinate picker to switch type before clicking
- Instruction banner updates dynamically to show the currently selected type
- Coordinate display shows `[D]` or `[R]` suffix for non-default types

### Game Profiles
- **Save** coordinates + click settings + delay as named presets
- **Load** presets into any game session
- **Export/Import** profiles as `.autoclick` files for sharing
- **Delete** profiles you no longer need
- Profiles persist across app sessions

### Pixel Color Guard
- Before each click, optionally **verify the pixel color** at the coordinate matches a reference color (captured when the point was picked)
- On mismatch: **Skip Point** or **Stop Session** (configurable)
- Adjustable **color tolerance** (0-50) for minor color variations
- Color swatches displayed next to coordinates when guard is enabled

### Scheduler
- Set **start time** and optional **stop time** (HH:mm format) for automated sessions
- Live **countdown display** in toolbar
- One-shot execution: starts at scheduled time, stops at scheduled time (or manually)
- Cancel anytime

### Real-Time Dashboard
- Dedicated **Dashboard tab** with interactive charts:
  - **CPM Line Chart** (live, 2-second intervals, 5-minute rolling window)
  - **Per-Game Breakdown** (horizontal bar chart: success vs skipped per game)
  - **Success/Skip Ratio** (pie chart)
  - **Per-Game CPM Timeline** (individual line per game with color coding and legend)
- **Summary cards**: Total Clicks, Skipped, Uptime, Clicks/min, Peak CPM
- **Export Stats** to JSON (full session data including exited games)
- **Reset All Stats** button (disabled while tasks run)
- Charts **freeze automatically** when no tasks are running

### Game Exit Notifications
- **Balloon notification** when a game process exits while tasks are running
- Shows success and skipped click counts for the exited game
- **Auto-stop** when all games have exited the queue

### Settings Mode
- **Global mode**: Same click settings for all games (default)
- **Custom mode**: Per-game intervals, click mode, and profiles

### Other Improvements
- **Archived game stats**: Exited game data preserved in dashboard until manual reset
- **Per-game click stats**: Each game card shows Success, Skipped, and Total counts
- **Bounds validation**: Auto-stop if window resizes and coordinates go out of bounds
- **Sound notifications** for all major actions (toggleable)

## Fixes
- Pixel color guard settings not syncing to existing sessions after saving
- Color swatches incorrectly shown when guard is disabled
- Per-game chart only displaying one process name with 2+ games
- ESC key handling in coordinate picker
- Peak CPM absurd values on first tick (inherited from v1.1.0)

## Important Notices

> **System Requirements:** Running multiple games simultaneously increases CPU, RAM, and GPU usage significantly. Ensure your system meets the minimum requirements specified by each game's developer. See [DISCLAIMER](docs/DISCLAIMER.md).

> **Anti-Cheat Warning:** Games with kernel-level anti-cheat may detect this tool. Use at your own risk. The developer is not responsible for any account bans.

> **Self-Contained Build:** This release includes the .NET 8 runtime. No separate runtime installation is required.

## Compatibility

| OS | v1.2.0 | Notes |
|----|:------:|-------|
| Windows 10 22H2+ / Windows 11 | **Full** | Recommended |
| Windows 8.1 | Partial | Requires [VC++ 2015 Redistributable](https://www.microsoft.com/en-us/download/details.aspx?id=48145). Dashboard not available; use v1.1.0 |
| Windows 7 | **Failed** | Not supported |

See [System Requirements](docs/SYSTEM_REQUIREMENTS.md) for tested configs and performance notes.

## Upgrade
If you are on v1.0.x or v1.1.0, this version includes all previous fixes. Auto-update from v1.0.3+ should work seamlessly.

---

**Full Changelog:** https://github.com/poli0981/autoclick/compare/v1.1.0...v1.2.0
