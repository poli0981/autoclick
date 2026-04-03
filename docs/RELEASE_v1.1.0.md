# v1.1.0

Major feature update with multi-point sequences, 3 new languages, live statistics, and sound notifications.

## New Features

### Multi-Point Click Sequences
- Add **multiple coordinates** per game, creating click sequences executed in order each cycle
- Display: `#1(100,200) → #2(300,400) → #3(500,600)`
- Configurable **delay between points** (0–10,000ms) — set per game card
- Click count now tracks **individual clicks**, not cycles (3 points = 3 clicks per cycle)
- "Add Point" replaces old "Select Coordinate"; "Clear Points" to reset all

### Game Dialog / Choice Compatibility
- `InputSimulator` now sends **`WM_MOUSEMOVE` + 15ms delay** before each click
- Fixes games where choice buttons, dialog options, and menus ignored PostMessage clicks
- Sequence: MouseMove → 15ms → MouseDown → 15ms → MouseUp

### 3 New Languages (5 total)
- 🇯🇵 **日本語** (Japanese)
- 🇰🇷 **한국어** (Korean)
- 🇨🇳 **中文** (Chinese Simplified)
- ~100 string keys per language. All AI-generated — community corrections welcome via PR

### Session Statistics Panel
- Live stats bar below toolbar: **Total Clicks**, **Uptime** (hh:mm:ss), **Clicks/min** (average), **Peak** CPM
- Appears after first game starts, updates every 2 seconds
- Resets on "Reset App"

### Sound Notifications
- System sounds for: Start, Stop, Pause/Resume, coordinate pick success, errors
- Uses Windows `SystemSounds` — no external audio files
- Toggle on/off in Settings > Advanced > "Sound Notifications"

## Fixes
- **Peak CPM** no longer shows absurd values on first tick (was dividing by near-zero time)

## Upgrade
If you are on v1.0.x, this version includes all previous fixes (hardcoded update URL, assembly metadata, etc.).

---

**Full Changelog:** https://github.com/poli0981/autoclick/compare/v1.0.3...v1.1.0

### Tested Games

| Game | Result |
|------|--------|
| [Bound Between Desks](https://store.steampowered.com/app/3790060/Bound_Between_Desks/) | Pass |
| [Wuthering Waves](https://store.steampowered.com/app/3513350/Wuthering_Waves/) | Pass (ACE Kernel — use at own risk) |
| [Myosotis](https://store.steampowered.com/app/4220830/Myosotis_My_Life_Is_Not_Yours_to_Take/) | Pass |
| [Man in a Suit...](https://store.steampowered.com/app/3292090/Man_in_a_suit_in_a_building_in_a_city/) | Pass |
| [Mercury Elopement Syndrome](https://store.steampowered.com/app/4417890/Mercury_Elopement_Syndrome/) | Fail (DirectInput) |
