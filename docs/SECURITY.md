# Security Policy

**Last updated:** April 2026

## Supported Versions

| Version | Supported |
|---------|-----------|
| Latest release | Yes |
| Previous releases | Security fixes only |

## Application Security

### Administrator Privileges
AutoClick requests administrator privileges (`requireAdministrator` in app manifest) for the following reasons:
- **RegisterHotKey**: Global hotkey registration requires elevated privileges
- **PostMessage to elevated processes**: Some games run as administrator; to send messages to them, AutoClick must also be elevated

### No Remote Code Execution
- The Software does not execute any remote code
- Updates are downloaded as complete packages from GitHub Releases and verified before installation
- Velopack handles update verification with checksum validation

### No Data Exfiltration
- The Software does not transmit any user data, game data, or system information to external servers
- The only network activity is optional update checking via GitHub API

### Local Storage
- Settings are stored in plain JSON at `%LocalAppData%\AutoClick\`
- No encryption is used because no sensitive data is stored
- Log files contain only application events, no personal data

## Reporting Vulnerabilities

If you discover a security vulnerability, please:

1. **Do NOT** open a public GitHub Issue
2. Contact the developer privately via GitHub ([@poli0981](https://github.com/poli0981))
3. Include a detailed description and steps to reproduce
4. Allow reasonable time for a fix before public disclosure

## Known Security Considerations

### Win32 API Usage
- `PostMessage` is used to send synthetic mouse events to game windows
- `EnumWindows` enumerates visible windows to detect running games
- `SetForegroundWindow` brings game windows to the front for coordinate picking
- These are standard Win32 APIs and do not bypass security boundaries

### Anti-Virus False Positives
Auto-clicker software may be flagged by antivirus programs as potentially unwanted software (PUP). This is a common false positive for automation tools. The source code is fully open for inspection.
