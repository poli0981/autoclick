## Description

<!-- Required: Clearly describe what this PR does and why. -->

## Reason

<!-- Required: Check all that apply. -->

- [ ] Bug fix
- [ ] New feature
- [ ] Performance improvement
- [ ] Refactoring / code cleanup
- [ ] Documentation update
- [ ] Translation / localization
- [ ] CI / build configuration
- [ ] Other: <!-- describe -->

## OS Version

<!-- Required: Your Windows version and build. -->

## Game Tested

<!-- Required: Steam/store link of the game you tested against, or "ClickTarget" if you used tools/AutoClick.ClickTarget. -->
<!-- Use a legitimate link. Suspicious links will cause this PR to be auto-ignored. -->

## Screenshots

<!-- Optional: Before/after screenshots or GIFs. -->

## Log Output

<!-- Optional: Relevant log entries from the app. -->

```text
```

## IDE / Build Environment

<!-- Optional: IDE and .NET SDK version used. -->

## Checklist

- [ ] `dotnet build AutoClick.slnx` and `dotnet test --solution AutoClick.slnx` pass (see [CONTRIBUTING](../CONTRIBUTING.md) for the other CI checks)
- [ ] Tested manually on a game or with `tools/AutoClick.ClickTarget`
- [ ] New UI text is in `Strings.resx` and in all four translations (`vi`, `ja`, `ko`, `zh`); no hard-coded text in XAML
- [ ] User-visible changes are listed under `[Unreleased]` in `CHANGELOG.md`
- [ ] No secrets, credentials, or suspicious code

---

> **Auto-Ignore Policy:** PRs will be automatically ignored if they:
>
> - Contain suspicious code or embedded malware (will be reviewed in a sandboxed VM)
> - Include links suspected of data theft or phishing
> - Discuss topics unrelated to this project
> - Are excessively verbose, vague, or off-topic
> - Insult or disrespect any contributor to this repository
>
> You may use this template or write your own, as long as required fields (Description, OS Version, Reason, Game Tested) are included.
