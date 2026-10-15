# Security Policy

**Last updated:** October 2026 (AutoClick 2.0)

## Supported versions

| Version | Supported |
|---------|-----------|
| 2.x (latest release) | Yes |
| 1.x | No: update to 2.x (it migrates your settings and profiles) |

AutoClick includes its own .NET runtime. When Microsoft publishes a .NET security update, a patch release of AutoClick is built with the updated runtime; installed copies receive it through the normal update.

## Reporting a vulnerability

Please **do not open a public issue** for security problems.

1. Report it privately through GitHub: [**Security → Report a vulnerability**](https://github.com/poli0981/autoclick/security/advisories/new).
2. If that is not possible, email the developer (address on the developer's [GitHub profile](https://github.com/poli0981) and in [social.md](social.md)).
3. Include the AutoClick version, steps to reproduce and the impact you expect.

You will get an answer as soon as possible. Please allow time for a fix before disclosing the problem publicly; you will be credited in the advisory unless you prefer otherwise.

## How AutoClick is built and shipped

- Every change goes through pull requests that must pass CodeQL, dependency review (no known-vulnerable or non-GPL-compatible packages), a daily audit of all direct and transitive packages, and OpenSSF Scorecard checks. GitHub Actions are pinned to commit hashes.
- Releases are built by the repository's release workflow from a protected `vX.Y.Z` tag. Each release includes an SPDX software bill of materials and a GitHub build provenance attestation. Releases are not code-signed; verify a download with:

  ```bash
  gh attestation verify AutoClick-win-Setup.exe -R poli0981/autoclick
  ```

- Updates come only from this repository's GitHub Releases over HTTPS, and Velopack checks each package against the hash in the release feed before applying it.

## Design notes

- **Privileges:** AutoClick runs as a standard user. Windows blocks input from a normal program into a program that runs as administrator (User Interface Privilege Isolation), so AutoClick only asks for administrator rights when you choose to click such a game. While it is elevated, links open through Explorer, so your browser does not inherit administrator rights.
- **Input:** clicks and keys are sent with `PostMessage` to the window you picked. AutoClick does not inject code, read game memory, or install hooks into other processes; it watches window focus and position with out-of-process accessibility events (`SetWinEventHook`).
- **Single instance:** the lock and the named pipe used to bring an existing window to the front are restricted to your Windows account.
- **Files:** settings and profiles are plain JSON in `%LocalAppData%\AutoClick` and are written atomically. Imported profile and session files are validated; a profile id can never be used to write outside the profiles folder.
- **No network** besides the update check (see the [Privacy Policy](PRIVACY_POLICY.md)).

## Antivirus false positives

Automation tools are sometimes flagged as potentially unwanted software. AutoClick's source code is public, and the attestation above proves that a download was built from it. If your antivirus flags an official release, please report it to the antivirus vendor as a false positive.
