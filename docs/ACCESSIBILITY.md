# Accessibility

**Last reviewed:** November 2026 (AutoClick 2.1)

AutoClick should work for everyone who wants to automate a game, including people who use only a keyboard, a screen reader, a contrast theme or large display scaling. This statement explains what AutoClick supports, what does not work yet, how it is tested, and how to report a barrier. It covers the AutoClick app for Windows, its installer and this repository's documentation.

## Goals

- **Target:** we work toward [WCAG 2.2](https://www.w3.org/TR/WCAG22/) Level AA, applied to desktop software as described in [WCAG2ICT](https://www.w3.org/TR/wcag2ict/). This is a goal, not a claim of conformance: AutoClick has not had a formal accessibility audit.
- **Keyboard:** every feature can be used without a mouse, with a visible focus indicator, a logical Tab order and no keyboard traps.
- **Screen readers:** every control has a name in the language of the interface, status changes and messages are announced, and page and section titles are headings. Narrator is the reference screen reader.
- **Vision:** contrast themes, display scaling on every monitor, and color never as the only way to tell something.
- **Motion:** no flashing content, and animations follow the Windows setting.

## Accessibility features

### Keyboard

| Where | Keys |
|-------|------|
| Anywhere in the window | `Tab` / `Shift+Tab` move between controls, `Enter` or `Space` activates a button, arrow keys move in lists and menus, `Esc` closes a dialog. The page list (Queue, Dashboard, Settings, About) is reached with `Tab`. |
| Global hotkeys | `F6` pause or resume all, `F7` stop all, `F8` start all. They work while the game is in front and can be changed in **Settings → Hotkeys**. |
| Coordinate picker (**Add step**) | Arrow keys move the pointer 10 pixels (`Shift`+arrow: 1 pixel) inside the game window; `Enter` or `Space` picks the point. `1`-`4` choose left, double or right click, or a key press; `Esc` cancels. After picking, AutoClick comes back to the front with the focus on **Add step**. |
| Key press steps | In the picker, press `4`, then the key to send. Every key is recorded in this mode, including the arrow keys and `Enter`. |
| Steps list | **Move up** and **Move down** reorder steps without dragging. **Random point** adds a click point without picking one. |
| Recording a hotkey | Press the new combination. `Tab` moves to the buttons and `Esc` cancels. |

### Screen readers

- Buttons that show only an icon, inputs, lists and list items have names, in every language of the interface. Each game card is named after its game, and each step after what it does (for example "Click at (40, 60), reference color #336699").
- These changes are announced without moving the focus:
  - the state of a game, and why a game stopped by itself;
  - the status bar and the update status;
  - schedule errors, and the key combination while you record a hotkey;
  - the coordinate picker's instructions;
  - messages that appear for a few seconds, and each step you add.
- Page titles are level 1 headings and section titles level 2 or 3, so the heading keys of a screen reader (in Narrator's scan mode: `H` and `Shift+H`) move between them.
- The scheduler's countdown is not announced every second; read it when you need it.

### Display

- **Themes:** *Use Windows setting* follows Windows' light, dark and contrast themes. *Light*, *Dark* and *High contrast* can also be chosen in AutoClick's **Settings → Appearance**.
- **Color is never the only signal:** game states, warnings and log levels also have text, and the reference color of a step is named for screen readers and shown as a hex value in its tooltip.
- **Scaling:** AutoClick is per-monitor DPI aware. It follows the Windows *Scale* setting, and the coordinate picker and the heatmap stay exact on every monitor.
- **Motion:** with *Animation effects* turned off in Windows (Windows 11: **Settings → Accessibility → Visual effects**; Windows 10: **Settings → Ease of Access → Display → Show animations**), pages no longer slide in and the dashboard charts no longer animate.
- **Sounds** are optional (the **Sounds** setting in AutoClick's Settings), and every sound has a visual or spoken equivalent.

## Supported environments

| Environment | Support |
|-------------|---------|
| Windows 10 version 2004 or later, Windows 11 (x64) | Supported |
| Keyboard only | Supported, including the coordinate picker |
| Narrator | Supported; the reference screen reader, checked before releases that change the interface |
| NVDA | Expected to work (AutoClick uses Windows UI Automation), not tested regularly |
| JAWS and other screen readers | Not tested |
| Voice Access and other speech input | Not tested; the control names it relies on are in place |
| Contrast themes and the High contrast theme | Supported |
| Display scaling | Supported at any scale, on every monitor |

## Known limitations

- **Windows *Text size* setting:** WPF applications do not receive it, so AutoClick's text keeps its size. Use the Windows *Scale* setting instead (**Settings → System → Display**), which AutoClick follows.
- **Dashboard charts** are drawings: screen readers read only their titles. The totals, clicks per minute, the peak and the active time are text on the same page, each game card shows its own numbers, and **Export statistics** saves the current totals as a CSV file.
- **Heatmap overlay:** it shows where clicks landed with colors and sizes only; its legend gives the totals as text.
- **Overlays:** the coordinate picker and the heatmap use their own fixed colors (a light blue tint, white text on a dark background), not the theme or a contrast theme.
- **Coordinate picker:** the coordinates next to the pointer are not read aloud while it moves; the step is announced once you pick it.
- **Notification area icon:** its menu has not been tested with a screen reader. The main window offers the same commands.
- **Translations** are AI-generated and may contain mistakes; corrections are welcome.
- **Games:** AutoClick cannot change how accessible a game itself is.

**If you meet a barrier that is not listed here, please [report it](#reporting-a-barrier).**

## How we test

| When | What |
|------|------|
| Every pull request and release build | The smoke test opens every page in every theme (including High contrast) and every language, plus the dialogs, and fails when a control that can be reached with the keyboard has no name, when a list item has no readable name, or when text does not follow the theme. Architecture tests check that all interface text is translated. |
| Before a release that changes the interface | A keyboard-only pass with the mouse disconnected, a Narrator pass of the changed screens, and a look at the High contrast theme. |

Recorded passes:

| Version | Pass |
|---------|------|
| 2.0.3 (October 2026) | Keyboard only (mouse disconnected) and Narrator, Windows 11, by the maintainer: no errors found. |
| 2.1.0 (November 2026) | Name check added to the smoke test (it found the problems fixed in 2.1.0). Keyboard picking and the announcements were checked through UI Automation, the interface Narrator uses. |

## Reporting a barrier

Open an [accessibility issue](https://github.com/poli0981/autoclick/issues/new?template=accessibility.yml). It helps to include:

- what you were trying to do, and where it failed;
- the steps to get there;
- your assistive technology and its version (for example Narrator, NVDA 2026.2, a switch device), or how you use the computer (keyboard only, magnifier, contrast theme);
- your Windows version and AutoClick version (shown on the About page);
- a screen recording or screenshot, if you can.

You never need to explain a disability or share personal information. If you prefer not to report in public, write to the developer by email (address on the [GitHub profile](https://github.com/poli0981) and in [social.md](social.md)) or on [Discord](https://discord.gg/kDM9GMu5vm).

### Severity

| Severity | Meaning |
|----------|---------|
| Critical | Blocks a core task, with no workaround (for example, a game cannot be added or started) |
| Serious | A core task is very hard, but a workaround exists |
| Moderate | Annoying or inconsistent, but the task can be done |
| Minor | Little effect on use |

### How we respond

- We acknowledge a report within a week, without questioning the reporter's experience.
- We explain what we will do next, and what we cannot change (for example, a limit of WPF or of a game).
- We suggest a workaround when there is one, and post updates in the issue.
- We ask the reporter to confirm the fix before closing, if they are willing.

AutoClick is maintained by one person in their spare time. From the day a report is opened, we aim to fix Critical barriers within 30 days, Serious ones within 60 days, and Moderate and Minor ones within 90 days. When a target is missed, the issue says why, what the workaround is, and when to expect a fix.

## Contributing

Interface changes follow the [accessibility rules in CONTRIBUTING](../CONTRIBUTING.md#accessibility), and the pull request template asks for a keyboard and Narrator check. Reports and suggestions from people who use assistive technology are especially welcome, in issues, in [Discussions](https://github.com/poli0981/autoclick/discussions) or as pull requests.

## Ownership

The maintainer ([poli0981](https://github.com/poli0981)) is responsible for accessibility: triaging reports, tracking barriers, and reviewing this statement with every minor release.

## Feedback

Accessibility is ongoing work. If something in this statement is unclear, wrong or missing, open an issue or a pull request.

This statement follows the guidance of [Open Source Guides](https://opensource.guide/accessibility-best-practices-for-your-project/#start-with-an-accessibility-statement) and the [Open Source Accessibility toolkit](https://github.com/open-source-accessibility/accessibility-toolkit/blob/main/ACCESSIBILITY.md).
