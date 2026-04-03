using System;
using System.Threading.Tasks;
using System.Windows;
using AutoClick.Core.Interfaces;
using AutoClick.UI.Resources;

namespace AutoClick.UI.ViewModels;

public class UpdateViewModel : ViewModelBase
{
    private readonly IUpdateService _updateService;
    private readonly ILogService _log;

    private string _statusText = "";
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

    private string _currentVersion = "";
    public string CurrentVersion { get => _currentVersion; set => SetProperty(ref _currentVersion, value); }

    private string _newVersion = "";
    public string NewVersion { get => _newVersion; set => SetProperty(ref _newVersion, value); }

    private int _downloadProgress;
    public int DownloadProgress { get => _downloadProgress; set => SetProperty(ref _downloadProgress, value); }

    private bool _isChecking;
    public bool IsChecking { get => _isChecking; set { SetProperty(ref _isChecking, value); OnPropertyChanged(nameof(CanCheck)); } }

    private bool _isDownloading;
    public bool IsDownloading { get => _isDownloading; set { SetProperty(ref _isDownloading, value); OnPropertyChanged(nameof(CanCheck)); OnPropertyChanged(nameof(CanDownload)); } }

    private bool _updateAvailable;
    public bool UpdateAvailable { get => _updateAvailable; set { SetProperty(ref _updateAvailable, value); OnPropertyChanged(nameof(CanDownload)); } }

    private bool _downloadComplete;
    public bool DownloadComplete { get => _downloadComplete; set { SetProperty(ref _downloadComplete, value); OnPropertyChanged(nameof(CanApply)); } }

    private bool _hasError;
    public bool HasError { get => _hasError; set => SetProperty(ref _hasError, value); }

    public bool CanCheck => !IsChecking && !IsDownloading;
    public bool CanDownload => UpdateAvailable && !IsDownloading && !DownloadComplete;
    public bool CanApply => DownloadComplete;

    public RelayCommand CheckForUpdateCommand { get; }
    public RelayCommand DownloadUpdateCommand { get; }
    public RelayCommand ApplyAndRestartCommand { get; }

    public UpdateViewModel(IUpdateService updateService, ILogService log)
    {
        _updateService = updateService;
        _log = log;

        CurrentVersion = _updateService.CurrentVersion;
        StatusText = _updateService.IsInstalled
            ? Strings.UpdateReady
            : Strings.UpdateDevMode;

        CheckForUpdateCommand = new RelayCommand(async () => await CheckForUpdateAsync(), () => CanCheck);
        DownloadUpdateCommand = new RelayCommand(async () => await DownloadUpdateAsync(), () => CanDownload);
        ApplyAndRestartCommand = new RelayCommand(OnApplyAndRestart, () => CanApply);
    }

    /// <summary>
    /// Automatically check and optionally download on startup if auto-update is enabled.
    /// </summary>
    public async Task AutoCheckOnStartupAsync()
    {
        if (!_updateService.IsInstalled)
        {
            _log.Info("Auto-update skipped: not an installed build.");
            return;
        }

        _log.Info("Auto-update: checking for updates on startup...");
        await CheckForUpdateAsync();

        if (UpdateAvailable)
        {
            _log.Info("Auto-update: update found, starting download...");
            await DownloadUpdateAsync();

            if (DownloadComplete)
            {
                _log.Info("Auto-update: download complete. Prompting user to restart.");
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    var result = MessageBox.Show(
                        string.Format(Strings.UpdateAutoDownloadedMsg, NewVersion),
                        Strings.UpdateAvailableTitle,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        OnApplyAndRestart();
                    }
                });
            }
        }
    }

    private async Task CheckForUpdateAsync()
    {
        IsChecking = true;
        HasError = false;
        StatusText = Strings.UpdateChecking;

        try
        {
            var result = await _updateService.CheckForUpdateAsync();

            if (result.Success)
            {
                if (result.UpdateAvailable)
                {
                    UpdateAvailable = true;
                    NewVersion = result.NewVersion ?? "";
                    StatusText = string.Format(Strings.UpdateFoundMsg, CurrentVersion, NewVersion);
                }
                else
                {
                    StatusText = Strings.UpdateUpToDate;
                }
            }
            else
            {
                HasError = true;
                StatusText = FormatErrorMessage(result);
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusText = $"Error: {ex.Message}";
            _log.Error("Update check exception", ex);
        }
        finally
        {
            IsChecking = false;
        }
    }

    private async Task DownloadUpdateAsync()
    {
        IsDownloading = true;
        HasError = false;
        DownloadProgress = 0;
        StatusText = Strings.UpdateDownloading;

        try
        {
            var result = await _updateService.DownloadUpdateAsync(progress =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    DownloadProgress = progress;
                    StatusText = $"{Strings.UpdateDownloading} {progress}%";
                });
            });

            if (result.Success)
            {
                DownloadComplete = true;
                StatusText = Strings.UpdateDownloadComplete;
            }
            else
            {
                HasError = true;
                StatusText = FormatErrorMessage(result);
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusText = $"Download error: {ex.Message}";
            _log.Error("Update download exception", ex);
        }
        finally
        {
            IsDownloading = false;
        }
    }

    private void OnApplyAndRestart()
    {
        StatusText = Strings.UpdateApplying;
        _updateService.ApplyUpdateAndRestart();
    }

    private static string FormatErrorMessage(UpdateCheckResult result)
    {
        return result.ErrorKind switch
        {
            UpdateErrorKind.NotInstalled => Strings.UpdateDevMode,
            UpdateErrorKind.NoRelease => Strings.UpdateNoRelease,
            UpdateErrorKind.RateLimited => Strings.UpdateRateLimited,
            UpdateErrorKind.Forbidden => Strings.UpdateForbidden,
            UpdateErrorKind.ServerError => Strings.UpdateServerError,
            UpdateErrorKind.NetworkError => Strings.UpdateNetworkError,
            UpdateErrorKind.ChecksumFailed => Strings.UpdateChecksumFailed,
            _ => result.ErrorMessage ?? Strings.UpdateUnknownError
        };
    }
}
