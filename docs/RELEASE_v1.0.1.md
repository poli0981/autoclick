# v1.0.1

Cleanup and localization fixes.

## What's Changed

### Fixed
- Vietnamese translations now use proper Unicode diacritics across all resource files
- GitHub repository URL default corrected in AppSettings (`poli0981/autoclick`)
- Document links in About view corrected to `blob/master`
- Removed orphaned `FreeRamCommand` (no UI binding existed)
- Removed unused `MaxRAM` / `FreeRAM` string resources

### Improved
- All remaining hardcoded English strings in XAML replaced with localized bindings:
  - "Remove All", "Random", "Reset", "Random Min (s)", "Random Max (s)"

### Added
- README with full project documentation
- CHANGELOG (v1.0.0 + v1.0.1)
- Code of Conduct with auto-ignore policy
- Developer Environment documentation (machine specs + tested games)
- GitHub Issue templates: Bug Report and Feature Request (YAML forms)
- Pull Request template with checklist and auto-ignore notice
- New localized string keys: `RemoveAll`, `RandomMin`, `RandomMax`, `ResetStats` (EN + VI)

---

### Tested Games

| Game | Result |
|------|--------|
| [Bound Between Desks](https://store.steampowered.com/app/3790060/Bound_Between_Desks/) | Pass |
| [Wuthering Waves](https://store.steampowered.com/app/3513350/Wuthering_Waves/) | Pass (ACE Kernel -- use at own risk) |
| [Myosotis](https://store.steampowered.com/app/4220830/Myosotis_My_Life_Is_Not_Yours_to_Take/) | Pass |
| [Man in a Suit...](https://store.steampowered.com/app/3292090/Man_in_a_suit_in_a_building_in_a_city/) | Pass |
| [Mercury Elopement Syndrome](https://store.steampowered.com/app/4417890/Mercury_Elopement_Syndrome/) | Fail (DirectInput) |

**Full Changelog:** https://github.com/poli0981/autoclick/compare/v1.0.0...v1.0.1
