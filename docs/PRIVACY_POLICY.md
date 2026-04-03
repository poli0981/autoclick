# Privacy Policy

**Last updated:** April 2026

## Overview

AutoClick is a desktop application that runs entirely on your local machine. We are committed to protecting your privacy.

## Data Collection

AutoClick does **NOT** collect, transmit, or store any personal data on external servers. Specifically:

- **No telemetry**: The application does not send usage data, crash reports, or analytics to any server.
- **No account required**: No registration, login, or personal information is needed to use the software.
- **No network requests**: The only outbound network requests are for checking application updates via the GitHub Releases API (`api.github.com`). This is optional and can be disabled in Settings.

## Local Data Storage

AutoClick stores the following data **locally** on your machine:

| Data | Location | Purpose |
|------|----------|---------|
| Settings | `%LocalAppData%\AutoClick\settings.json` | User preferences (click mode, intervals, theme, language, hotkeys) |
| Logs | `%LocalAppData%\AutoClick\logs\` | Application debug logs for troubleshooting |

No game credentials, personal identifiers, or sensitive information is stored.

## Update Mechanism

When auto-update is enabled, AutoClick contacts the GitHub Releases API to check for new versions. This request includes:
- Your IP address (standard HTTP request, handled by GitHub's privacy policy)
- No authentication tokens for public repositories

You may disable auto-update in Settings at any time.

## Third-Party Services

- **GitHub API**: Used solely for update checking. Subject to [GitHub's Privacy Statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement).

## Children's Privacy

AutoClick does not knowingly collect any data from children or any other users.

## Changes to This Policy

We may update this Privacy Policy from time to time. Changes will be posted in the GitHub repository.

## Contact

For privacy-related questions, please open an issue on the [GitHub repository](https://github.com/poli0981/autoclick).
