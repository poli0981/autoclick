using System.Runtime.InteropServices;
using System.Windows.Threading;
using AutoClick.Core.Abstractions;
using AutoClick.Services.Updates;
using AutoClick.UI.Localization;
using AutoClick.UI.Services;
using AutoClick.UI.Startup;
using AutoClick.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoClick.UI.ViewModels;

/// <summary>Version, updates, documents and links.</summary>
public sealed partial class AboutViewModel : ObservableObject
{
    private readonly UpdateService _updates;
    private readonly AppLifetime _lifetime;
    private readonly ShellLauncher _shell;
    private readonly DialogService _dialogs;
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

    public AboutViewModel(UpdateService updates, AppLifetime lifetime, ShellLauncher shell, DialogService dialogs, IElevationService elevation)
    {
        _updates = updates;
        _lifetime = lifetime;
        _shell = shell;
        _dialogs = dialogs;
        IsElevated = elevation.IsCurrentProcessElevated;
        updates.Changed += (_, _) => _dispatcher.BeginInvoke(RefreshUpdate);
        LocalizationService.Instance.LanguageChanged += (_, _) => RefreshUpdate();
        RefreshUpdate();
    }

    public string Version { get; } = AppInfo.Version;

    public string Runtime { get; } = RuntimeInformation.FrameworkDescription;

    public string OperatingSystem { get; } = RuntimeInformation.OSDescription;

    public bool IsElevated { get; }

    [ObservableProperty]
    public partial string UpdateStatus { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsUpdateBusy { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    public partial bool CanCheck { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadUpdateCommand))]
    public partial bool CanDownload { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyUpdateCommand))]
    public partial bool CanApply { get; set; }

    [ObservableProperty]
    public partial string? ReleaseNotes { get; set; }

    [RelayCommand(CanExecute = nameof(CanCheck))]
    public Task CheckForUpdatesAsync() => _updates.CheckAsync();

    [RelayCommand(CanExecute = nameof(CanDownload))]
    public Task DownloadUpdateAsync() => _updates.DownloadAsync();

    [RelayCommand(CanExecute = nameof(CanApply))]
    public Task ApplyUpdateAsync() => _lifetime.ApplyUpdateAndRestartAsync();

    [RelayCommand]
    public void Open(string? target)
    {
        if (!string.IsNullOrEmpty(target) && !_shell.TryOpen(target))
        {
            _dialogs.Toast(L.Get("About_Title"), L.Format("Common_OpenFailed", target), ToastKind.Warning);
        }
    }

    /// <summary>Opens a file shipped next to the executable (license, third-party notices).</summary>
    [RelayCommand]
    public void OpenBundledFile(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        var path = Path.Combine(AppContext.BaseDirectory, fileName);
        if (File.Exists(path))
        {
            Open(path);
        }
        else
        {
            // Development builds have no generated notices: show the online copy.
            Open(AppLinks.Acknowledgements);
        }
    }

    private void RefreshUpdate()
    {
        var state = _updates.State;
        IsUpdateBusy = state is UpdateState.Checking or UpdateState.Downloading;
        CanCheck = state is UpdateState.Idle or UpdateState.UpToDate or UpdateState.Failed;
        CanDownload = state == UpdateState.Available;
        CanApply = state == UpdateState.ReadyToApply;
        ReleaseNotes = state is UpdateState.Available or UpdateState.Downloading or UpdateState.ReadyToApply ? _updates.ReleaseNotes : null;
        UpdateStatus = state switch
        {
            UpdateState.NotInstalled => L.Get("Update_NotInstalled"),
            UpdateState.Checking => L.Get("Update_Checking"),
            UpdateState.UpToDate => L.Get("Update_UpToDate"),
            UpdateState.Available => L.Format("Update_Available", _updates.NewVersion),
            UpdateState.Downloading => L.Format("Update_Downloading", _updates.DownloadProgress),
            UpdateState.ReadyToApply => L.Format("Update_Ready", _updates.NewVersion),
            UpdateState.Failed => _updates.Error switch
            {
                UpdateError.NoRelease => L.Get("Update_Error_NoRelease"),
                UpdateError.RateLimited => L.Get("Update_Error_RateLimited"),
                UpdateError.Forbidden => L.Get("Update_Error_Forbidden"),
                UpdateError.ServerError => L.Get("Update_Error_Server"),
                UpdateError.Network => L.Get("Update_Error_Network"),
                _ => L.Get("Update_Error_Unknown"),
            },
            _ => L.Get("Update_Idle"),
        };
    }
}
