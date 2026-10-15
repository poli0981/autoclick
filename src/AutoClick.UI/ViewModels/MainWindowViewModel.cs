using System.Windows.Threading;
using AutoClick.Core.Abstractions;
using AutoClick.Services.Persistence;
using AutoClick.Services.Updates;
using AutoClick.UI.Localization;
using AutoClick.UI.Services;
using AutoClick.UI.Startup;
using AutoClick.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoClick.UI.ViewModels;

/// <summary>Window title, status bar and the app-wide notices (info bars).</summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly SettingsStore _settings;
    private readonly AppLifetime _lifetime;
    private readonly UpdateService _updates;
    private readonly DialogService _dialogs;
    private readonly ShellLauncher _shell;
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

    public MainWindowViewModel(
        AppHostContext context,
        AppLifetime lifetime,
        UpdateService updates,
        DialogService dialogs,
        ShellLauncher shell,
        IElevationService elevation,
        QueueViewModel queue)
    {
        _settings = context.Settings;
        _lifetime = lifetime;
        _updates = updates;
        _dialogs = dialogs;
        _shell = shell;
        Queue = queue;
        IsElevated = elevation.IsCurrentProcessElevated;
        CorruptSettingsBackup = context.Settings.CorruptBackupPath;

        var settings = context.Settings.Current;
        ShowAdminNotice = settings.ShowAdminModeNotice && settings.AlwaysRunAsAdmin;
        ShowSettingsRecovered = context.SettingsLoadResult == SettingsLoadResult.RecoveredFromCorruption;
        ShowElevationDeclined = context.ElevationDeclined;

        queue.ElevationRequired += (_, game) =>
        {
            ElevationGame = game.ProcessName;
            ShowElevationRequired = !IsElevated;
        };
        updates.Changed += (_, _) => _dispatcher.BeginInvoke(RefreshUpdate);
        LocalizationService.Instance.LanguageChanged += (_, _) => RefreshTexts();
        RefreshUpdate();
    }

    public QueueViewModel Queue { get; }

    public bool IsElevated { get; }

    public string Title => IsElevated ? $"AutoClick {AppInfo.Version} ({L.Get("Status_Administrator")})" : $"AutoClick {AppInfo.Version}";

    public string VersionText { get; } = "v" + AppInfo.Version;

    public string? CorruptSettingsBackup { get; }

    /// <summary>Users upgraded from v1 still start as administrator; tell them once that it is optional now.</summary>
    [ObservableProperty]
    public partial bool ShowAdminNotice { get; set; }

    [ObservableProperty]
    public partial bool ShowElevationRequired { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ElevationRequiredMessage))]
    public partial string ElevationGame { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowElevationDeclined { get; set; }

    [ObservableProperty]
    public partial bool ShowSettingsRecovered { get; set; }

    [ObservableProperty]
    public partial bool ShowUpdateReady { get; set; }

    [ObservableProperty]
    public partial string UpdateReadyMessage { get; set; } = string.Empty;

    public string ElevationRequiredMessage => L.Format("Notice_ElevationRequired", ElevationGame);

    public string SettingsRecoveredMessage => L.Format("Notice_SettingsRecovered", CorruptSettingsBackup ?? "-");

    [RelayCommand]
    public async Task RestartAsAdministratorAsync()
    {
        if (!await _lifetime.RestartAsAdministratorAsync())
        {
            _dialogs.Toast(L.Get("Admin_Restart"), L.Get("Admin_RestartCancelled"), ToastKind.Warning);
        }
    }

    [RelayCommand]
    public void TurnOffAdminMode()
    {
        _settings.Update(s => s with { AlwaysRunAsAdmin = false, ShowAdminModeNotice = false });
        ShowAdminNotice = false;
        _dialogs.Toast(L.Get("Notice_AdminTitle"), L.Get("Notice_AdminTurnedOff"), ToastKind.Success);
    }

    [RelayCommand]
    public Task ApplyUpdateAsync() => _lifetime.ApplyUpdateAndRestartAsync();

    [RelayCommand]
    public void OpenSettingsBackup()
    {
        if (CorruptSettingsBackup is { } path && Path.GetDirectoryName(path) is { } folder)
        {
            _shell.TryOpen(folder);
        }
    }

    partial void OnShowAdminNoticeChanged(bool value)
    {
        // Closing the notice (or choosing an action) means "do not show it again".
        if (!value && _settings.Current.ShowAdminModeNotice)
        {
            _settings.Update(s => s with { ShowAdminModeNotice = false });
        }
    }

    private void RefreshUpdate()
    {
        ShowUpdateReady = _updates.State == UpdateState.ReadyToApply;
        UpdateReadyMessage = L.Format("Notice_UpdateReady", _updates.NewVersion);
    }

    private void RefreshTexts()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(ElevationRequiredMessage));
        OnPropertyChanged(nameof(SettingsRecoveredMessage));
        RefreshUpdate();
    }
}
