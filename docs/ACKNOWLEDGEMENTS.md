# Acknowledgements & Third-Party Licenses

**Last updated:** October 2026 (AutoClick 2.0)

AutoClick is free software under the [GNU General Public License v3.0](../LICENSE). It is built on the open-source projects below. The complete list of every package in a release, with its license and copyright notice and the full license texts, is in `THIRD-PARTY-NOTICES.txt` next to `AutoClick.UI.exe` (also on **About → Third-party notices**). Every license is checked against a GPL-3.0-compatible allow list on each change.

## Runtime and interface

| Project | License | Used for |
|---------|---------|----------|
| [.NET and WPF](https://github.com/dotnet/wpf) | MIT | Runtime (included in the app) and UI framework |
| [WPF UI](https://github.com/lepoco/wpfui) (lepo.co) | MIT | Fluent controls, navigation, dialogs, themes |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | MIT | MVVM: observable properties and commands |
| [H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon) | MIT | Notification-area (tray) icon and balloon messages |
| [LiveCharts2](https://github.com/beto-rodriguez/LiveCharts2) | MIT | Dashboard charts |
| [SkiaSharp](https://github.com/mono/SkiaSharp) and HarfBuzzSharp | MIT | Chart rendering and text shaping |
| [OpenTK](https://github.com/opentk/opentk) and GLWpfControl | MIT | Pulled in by SkiaSharp's WPF views (not used for drawing) |
| [GLFW](https://www.glfw.org/) (via OpenTK.redist.glfw) | Zlib | Pulled in by OpenTK |

## Services

| Project | License | Used for |
|---------|---------|----------|
| [Microsoft.Extensions.Hosting](https://github.com/dotnet/runtime) | MIT | Startup, dependency injection, logging abstractions |
| [Serilog](https://serilog.net/) with Sinks.File, Sinks.Async and Extensions.Hosting | Apache-2.0 | Log files and the in-app log |
| [Velopack](https://velopack.io/) | MIT | Installer and updates from GitHub Releases |
| [CsWin32](https://github.com/microsoft/CsWin32) | MIT | Generates the Win32 interop code at build time (not shipped) |

## Development and testing (not shipped)

xUnit v3, Microsoft.Testing.Platform and its code coverage extension, Microsoft.Extensions.TimeProvider.Testing, ArchUnitNET, ReSharper Command Line Tools (JetBrains), nuget-license, Microsoft SBOM Tool, ReportGenerator, CodeQL, actionlint and zizmor.

## AI assistance

AutoClick is developed with the assistance of **Claude** (Anthropic) for code, architecture, reviews, documentation and translations. The developer reviews and tests the results.

## Translations

The in-app translations are AI-generated and may contain mistakes; corrections are welcome as pull requests to `src/AutoClick.UI/Resources/Strings.<language>.resx`.

| Language | Status |
|----------|--------|
| English | Primary |
| Tiếng Việt | AI-generated, developer-reviewed |
| 日本語 (Japanese) | AI-generated |
| 한국어 (Korean) | AI-generated |
| 中文 (Chinese, Simplified) | AI-generated |

## Special thanks

- The .NET, WPF and WPF UI communities
- The maintainers of every library listed above
- Everyone who reports bugs and suggests features
