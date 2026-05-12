using System.Diagnostics;
using System.Reflection;
using AutoClick.Core.Interfaces;

namespace AutoClick.UI.ViewModels;

public class AboutViewModel : ViewModelBase
{
    private const string RepoBase = "https://github.com/poli0981/autoclick";

    public string AppName => "AutoClick";
    public string Version =>
        Assembly.GetExecutingAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetExecutingAssembly()?.GetName().Version?.ToString()
        ?? "dev";
    public string Copyright =>
        Assembly.GetExecutingAssembly()
            ?.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright
        ?? "Copyright (c) 2026 poli0981";
    public string License => "GPL-3.0 License";
    public string Developer => "poli0981";
    public string Description => "Auto-click utility for games. Helps automate repetitive clicking without modifying game source code.";

    public string ThirdParty => string.Join(Environment.NewLine, new[]
    {
        "Serilog - Apache 2.0 License",
        "Microsoft.Extensions.DependencyInjection - MIT License",
        "Velopack - MIT License",
        ".NET 8 / WPF - MIT License"
    });

    public UpdateViewModel? UpdateVm { get; private set; }

    // Commands - documents
    public RelayCommand OpenGitHubCommand { get; }
    public RelayCommand OpenLicenseCommand { get; }
    public RelayCommand OpenPrivacyPolicyCommand { get; }
    public RelayCommand OpenDisclaimerCommand { get; }
    public RelayCommand OpenAcknowledgementsCommand { get; }
    public RelayCommand OpenTermsCommand { get; }
    public RelayCommand OpenEulaCommand { get; }
    public RelayCommand OpenSecurityCommand { get; }

    // Commands - Connect & Support (v1.3.3)
    public RelayCommand OpenXCommand { get; }
    public RelayCommand OpenYouTubeCommand { get; }
    public RelayCommand OpenDiscordCommand { get; }
    public RelayCommand OpenPatreonCommand { get; }
    public RelayCommand OpenKofiCommand { get; }
    public RelayCommand OpenBlueskyCommand { get; }
    public RelayCommand OpenMastodonCommand { get; }
    public RelayCommand OpenSteamCommand { get; }
    public RelayCommand OpenEmailCommand { get; }
    public RelayCommand OpenDonateCommand { get; }
    public RelayCommand OpenReportBugCommand { get; }

    public AboutViewModel()
    {
        OpenGitHubCommand = new RelayCommand(() => OpenUrl(RepoBase));
        OpenLicenseCommand = new RelayCommand(() => OpenUrl($"{RepoBase}/blob/master/LICENSE"));
        OpenPrivacyPolicyCommand = new RelayCommand(() => OpenUrl($"{RepoBase}/blob/master/docs/PRIVACY_POLICY.md"));
        OpenDisclaimerCommand = new RelayCommand(() => OpenUrl($"{RepoBase}/blob/master/docs/DISCLAIMER.md"));
        OpenAcknowledgementsCommand = new RelayCommand(() => OpenUrl($"{RepoBase}/blob/master/docs/ACKNOWLEDGEMENTS.md"));
        OpenTermsCommand = new RelayCommand(() => OpenUrl($"{RepoBase}/blob/master/docs/TERMS_OF_SERVICE.md"));
        OpenEulaCommand = new RelayCommand(() => OpenUrl($"{RepoBase}/blob/master/docs/EULA.md"));
        OpenSecurityCommand = new RelayCommand(() => OpenUrl($"{RepoBase}/blob/master/docs/SECURITY.md"));

        OpenXCommand = new RelayCommand(() => OpenUrl("https://x.com/SkullMute0011"));
        OpenYouTubeCommand = new RelayCommand(() => OpenUrl("https://www.youtube.com/@SkullMute"));
        OpenDiscordCommand = new RelayCommand(() => OpenUrl("https://discord.gg/2aNR3aVt"));
        OpenPatreonCommand = new RelayCommand(() => OpenUrl("https://www.patreon.com/skullmute"));
        OpenKofiCommand = new RelayCommand(() => OpenUrl("https://ko-fi.com/skullmute"));
        OpenBlueskyCommand = new RelayCommand(() => OpenUrl("https://bsky.app/profile/skullmute0011.bsky.social"));
        OpenMastodonCommand = new RelayCommand(() => OpenUrl("https://mastodon.social/@skullmute1122"));
        OpenSteamCommand = new RelayCommand(() => OpenUrl("https://steamcommunity.com/profiles/76561199544666292/"));
        OpenEmailCommand = new RelayCommand(() => OpenUrl("mailto:lopop05905@proton.me"));
        OpenDonateCommand = new RelayCommand(() => OpenUrl("https://github.com/sponsors/poli0981"));
        OpenReportBugCommand = new RelayCommand(() => OpenUrl($"{RepoBase}/issues/new?template=bug_report.yml"));
    }

    /// <summary>
    /// Initialize the update VM (called after DI resolves dependencies).
    /// </summary>
    public void InitializeUpdate(IUpdateService updateService, ILogService logService)
    {
        UpdateVm = new UpdateViewModel(updateService, logService);
        OnPropertyChanged(nameof(UpdateVm));
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }
}
