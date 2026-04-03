using System.Diagnostics;
using System.Reflection;
using AutoClick.Core.Interfaces;

namespace AutoClick.UI.ViewModels;

public class AboutViewModel : ViewModelBase
{
    private const string RepoBase = "https://github.com/poli0981/autoclick";

    public string AppName => "AutoClick";
    public string Version => Assembly.GetExecutingAssembly()?.GetName().Version?.ToString() ?? "1.0.0";
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

    // Commands
    public RelayCommand OpenGitHubCommand { get; }
    public RelayCommand OpenLicenseCommand { get; }
    public RelayCommand OpenPrivacyPolicyCommand { get; }
    public RelayCommand OpenDisclaimerCommand { get; }
    public RelayCommand OpenAcknowledgementsCommand { get; }
    public RelayCommand OpenTermsCommand { get; }
    public RelayCommand OpenEulaCommand { get; }
    public RelayCommand OpenSecurityCommand { get; }

    public AboutViewModel()
    {
        OpenGitHubCommand = new RelayCommand(() => OpenUrl(RepoBase));
        OpenLicenseCommand = new RelayCommand(() => OpenUrl($"{RepoBase}/blob/main/LICENSE"));
        OpenPrivacyPolicyCommand = new RelayCommand(() => OpenUrl($"{RepoBase}/blob/main/docs/PRIVACY_POLICY.md"));
        OpenDisclaimerCommand = new RelayCommand(() => OpenUrl($"{RepoBase}/blob/main/docs/DISCLAIMER.md"));
        OpenAcknowledgementsCommand = new RelayCommand(() => OpenUrl($"{RepoBase}/blob/main/docs/ACKNOWLEDGEMENTS.md"));
        OpenTermsCommand = new RelayCommand(() => OpenUrl($"{RepoBase}/blob/main/docs/TERMS_OF_SERVICE.md"));
        OpenEulaCommand = new RelayCommand(() => OpenUrl($"{RepoBase}/blob/main/docs/EULA.md"));
        OpenSecurityCommand = new RelayCommand(() => OpenUrl($"{RepoBase}/blob/main/docs/SECURITY.md"));
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
