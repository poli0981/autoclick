# Privacy Policy

**Last updated:** October 2026 (AutoClick 2.0)

## Summary

AutoClick runs entirely on your PC. It has no accounts, no telemetry, no analytics and no crash reporting. The only network traffic is the update check against this project's GitHub Releases, which you can turn off.

## Data stored on your PC

Everything is kept in `%LocalAppData%\AutoClick` (for example `C:\Users\<you>\AppData\Local\AutoClick`):

| Data | Location | Contents |
|------|----------|----------|
| Settings | `settings.v2.json` | Your preferences: timing, pixel color guard, hotkeys, theme, language, window position |
| 1.x settings | `settings.json`, `backup\settings.v1.json` | Left from AutoClick 1.x (kept so 1.x keeps working, and a backup made during the upgrade) |
| Profiles | `profiles\*.json` | The steps and timing you saved |
| Logs | `logs\autoclick-YYYYMMDD.log` | Application events for troubleshooting, in daily files of at most 10 MB; the 14 most recent files are kept |
| Damaged files | `settings.v2.json.corrupt-*` | A settings file that could not be read, kept instead of being deleted |

The logs contain the process names and window titles of the games you add, click coordinates, file paths you import from or export to, and error details. A window title can contain personal text if you add a window such as a browser or a chat app; check a log before sharing it in a bug report.

Files you export (settings, profiles, sessions, statistics, logs) are written only where you choose.

**Uninstalling AutoClick deletes the whole `%LocalAppData%\AutoClick` folder**, including profiles and logs. The portable version keeps its data in the same folder; delete it yourself if you no longer need it.

## Network access

- **Update check:** when *Update automatically* is on (the default), AutoClick asks GitHub for the latest release at startup and every 6 hours, and downloads an update when there is one. With the setting off, this only happens when you press *Check for updates*. The requests go to GitHub (`api.github.com`, `github.com` and its release download hosts) and carry the information any web request does, such as your IP address. They do not identify you to the developer. See [GitHub's Privacy Statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement).
- **Links:** buttons on the About page open web pages in your browser. Nothing is sent until you click them.

AutoClick sends nothing else: no usage data, no settings, no logs.

## Children's privacy

AutoClick collects no data from anyone, including children.

## Changes to this policy

Changes are published in the [GitHub repository](https://github.com/poli0981/autoclick) and noted in the [CHANGELOG](../CHANGELOG.md).

## Contact

Open an issue on the [GitHub repository](https://github.com/poli0981/autoclick/issues) for privacy questions.
