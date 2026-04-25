using System.Windows;
using System.Windows.Interop;
using AutoClick.Core.Enums;
using AutoClick.Core.Models;
using AutoClick.Services;
using AutoClick.UI.Resources;
using AutoClick.UI.ViewModels;
using Microsoft.Win32;
using static AutoClick.Win32.NativeMethods;

namespace AutoClick.UI.Views;

public partial class MainWindow : Window
{
    private MainViewModel _mainVm = null!;
    private SettingsViewModel _settingsVm = null!;
    private DashboardViewModel _dashboardVm = null!;
    private HotkeyService? _hotkeyService;
    private SoundService? _soundService;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void Initialize(MainViewModel mainVm, SettingsViewModel settingsVm, AboutViewModel aboutVm, DashboardViewModel dashboardVm, HotkeyService hotkeyService, SoundService soundService)
    {
        _mainVm = mainVm;
        _settingsVm = settingsVm;
        _dashboardVm = dashboardVm;
        _hotkeyService = hotkeyService;
        _soundService = soundService;

        DataContext = mainVm;

        var settingsView = new SettingsView { DataContext = settingsVm };
        SettingsContent.Content = settingsView;

        var aboutView = new AboutView { DataContext = aboutVm };
        AboutContent.Content = aboutView;

        var dashboardView = new DashboardView { DataContext = dashboardVm };
        DashboardContent.Content = dashboardView;

        mainVm.RequestAddGame += OnRequestAddGame;

        // Sync ShowRealTimeLogs from settings -> main view
        settingsVm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SettingsViewModel.ShowRealTimeLogs))
                mainVm.UpdateShowLogs(settingsVm.ShowRealTimeLogs);
        };

        // Save button in settings -> save + reload
        settingsVm.SaveRequested += () => mainVm.SaveSettings();

        // Settings mode changed -> propagate to game sessions
        settingsVm.SettingsModeChanged += () =>
        {
            var isCustom = settingsVm.IsCustomMode;
            foreach (var g in mainVm.GameSessions)
                g.IsCustomMode = isCustom;
        };

        // Reset app -> refresh settings UI
        mainVm.SettingsReloaded += () => settingsVm.RefreshAllBindings();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);

        if (_hotkeyService != null)
        {
            _hotkeyService.Register("PauseResume", _settingsVm.HotkeyPauseResume, hwnd);
            _hotkeyService.Register("StopAll", _settingsVm.HotkeyStopAll, hwnd);
            _hotkeyService.Register("StartAll", _settingsVm.HotkeyStartAll, hwnd);
            _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == (int)WM_HOTKEY)
        {
            _hotkeyService?.HandleHotkeyMessage(wParam.ToInt32());
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void OnHotkeyPressed(string id)
    {
        Dispatcher.Invoke(() =>
        {
            switch (id)
            {
                case "PauseResume":
                    foreach (var g in _mainVm.GameSessions)
                    {
                        if (g.IsRunning) g.Pause();
                        else if (g.IsPaused) g.Resume();
                    }
                    _soundService?.PlayToggle();
                    break;
                case "StopAll":
                    _mainVm.StopAllCommand.Execute(null);
                    _soundService?.PlayStop();
                    break;
                case "StartAll":
                    _mainVm.StartAllCommand.Execute(null);
                    _soundService?.PlayStart();
                    break;
            }
        });
    }

    private void OnRequestAddGame()
    {
        var windows = _mainVm.GetAvailableWindows();
        var dialog = new GamePickerDialog(windows, _mainVm.GameDetector) { Owner = this };
        if (dialog.ShowDialog() == true && dialog.SelectedWindow != null)
        {
            _mainVm.TryAddGameSession(dialog.SelectedWindow);
        }
    }

    private void OnPickCoordinate(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is GameSessionViewModel sessionVm)
        {
            // Bring game window to front so user can see it
            SetForegroundWindow(sessionVm.Session.WindowHandle);

            // Brief delay to let the window come to front
            System.Threading.Thread.Sleep(200);

            var picker = new CoordinatePickerWindow(sessionVm.Session.WindowHandle);
            if (picker.ShowDialog() == true && picker.SelectedPoint != null)
            {
                // Only keep reference color when pixel color guard is enabled
                if (!sessionVm.Session.EnablePixelColorGuard)
                    picker.SelectedPoint.ReferenceColor = 0xFFFFFFFF;

                if (_mainVm.TryAddCoordinate(sessionVm, picker.SelectedPoint))
                {
                    _soundService?.PlaySuccess();
                }
                else
                {
                    _soundService?.PlayError();
                    MessageBox.Show(
                        Strings.CoordinateErrorMsg,
                        Strings.CoordinateErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }
    }

    private void OnClearCoordinates(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is GameSessionViewModel sessionVm)
        {
            _mainVm.ClearCoordinates(sessionVm);
        }
    }

    private void OnRandomCoordinate(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is GameSessionViewModel sessionVm)
        {
            if (_mainVm.TryAddRandomCoordinate(sessionVm))
            {
                _soundService?.PlaySuccess();
            }
            else
            {
                _soundService?.PlayError();
                MessageBox.Show(
                    Strings.RandomCoordinateErrorMsg,
                    Strings.RandomCoordinateErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }

    // ── Profile Event Handlers ──

    private void OnSaveProfile(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is GameSessionViewModel sessionVm)
        {
            if (!sessionVm.HasCoordinate)
            {
                MessageBox.Show(Strings.NoCoordinatesToSave, Strings.Profile,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new ProfileNameDialog { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                var name = dialog.ProfileName;
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show(Strings.ProfileNameEmpty, Strings.Profile,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var existing = _mainVm.SavedProfiles.FirstOrDefault(p =>
                    string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    var result = MessageBox.Show(
                        string.Format(Strings.ProfileNameDuplicate, name),
                        Strings.Profile, MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }

                _mainVm.SaveProfileFromSession(sessionVm, name);
                _soundService?.PlaySuccess();
            }
        }
    }

    private void OnLoadProfile(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is GameSessionViewModel sessionVm)
        {
            // Find the ComboBox in the same parent
            var parent = btn.Parent as System.Windows.Controls.StackPanel;
            var combo = parent?.Children.OfType<System.Windows.Controls.ComboBox>().FirstOrDefault();
            if (combo?.SelectedItem is GameProfile profile)
            {
                _mainVm.LoadProfileIntoSession(sessionVm, profile);
                _soundService?.PlaySuccess();
            }
        }
    }

    private void OnExportProfile(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is GameSessionViewModel)
        {
            var parent = btn.Parent as System.Windows.Controls.StackPanel;
            var combo = parent?.Children.OfType<System.Windows.Controls.ComboBox>().FirstOrDefault();
            if (combo?.SelectedItem is GameProfile profile)
            {
                var dlg = new SaveFileDialog
                {
                    Filter = "AutoClick Profile (*.autoclick)|*.autoclick",
                    FileName = $"{profile.Name}.autoclick"
                };
                if (dlg.ShowDialog() == true)
                {
                    _mainVm.ExportProfile(profile, dlg.FileName);
                    _soundService?.PlaySuccess();
                }
            }
        }
    }

    private void OnImportProfile(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is GameSessionViewModel)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "AutoClick Profile (*.autoclick)|*.autoclick|JSON files (*.json)|*.json"
            };
            if (dlg.ShowDialog() == true)
            {
                var dupName = _mainVm.CheckImportDuplicateName(dlg.FileName);
                if (dupName != null)
                {
                    var result = MessageBox.Show(
                        string.Format(Strings.ProfileNameDuplicate, dupName),
                        Strings.Profile, MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes)
                        return;
                }

                var profile = _mainVm.ImportProfile(dlg.FileName);
                if (profile != null)
                    _soundService?.PlaySuccess();
                else
                    _soundService?.PlayError();
            }
        }
    }

    private void OnDeleteProfile(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is GameSessionViewModel)
        {
            var parent = btn.Parent as System.Windows.Controls.StackPanel;
            var combo = parent?.Children.OfType<System.Windows.Controls.ComboBox>().FirstOrDefault();
            if (combo?.SelectedItem is GameProfile profile)
            {
                var result = MessageBox.Show(
                    string.Format(Strings.ConfirmDeleteProfile, profile.Name),
                    Strings.Profile, MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    _mainVm.DeleteProfile(profile.Id);
                    combo.SelectedItem = null;
                }
            }
        }
    }

    // ── Drag-drop reorder of click points ──

    private const string PointChipDragFormat = "AutoClick.ClickPointChip";

    private void OnPointChipMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;
        if (fe.DataContext is not ClickPoint point) return;
        var vm = FindAncestorDataContext<GameSessionViewModel>(fe);
        if (vm == null || !vm.IsIdle) return;

        var srcIndex = vm.ClickPoints.IndexOf(point);
        if (srcIndex < 0) return;

        var data = new System.Windows.DataObject();
        data.SetData(PointChipDragFormat, new PointChipDragPayload(vm, srcIndex));
        System.Windows.DragDrop.DoDragDrop(fe, data, System.Windows.DragDropEffects.Move);
    }

    private void OnPointChipDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;
        if (e.Data.GetData(PointChipDragFormat) is not PointChipDragPayload payload) return;
        if (fe.DataContext is not ClickPoint targetPoint) return;
        var targetVm = FindAncestorDataContext<GameSessionViewModel>(fe);
        if (targetVm == null || !ReferenceEquals(targetVm, payload.SourceVm)) return; // confine to same game

        var dstIndex = targetVm.ClickPoints.IndexOf(targetPoint);
        if (dstIndex < 0) return;

        targetVm.MoveClickPoint(payload.SourceIndex, dstIndex);
    }

    private static T? FindAncestorDataContext<T>(DependencyObject start) where T : class
    {
        var current = start;
        while (current != null)
        {
            if (current is FrameworkElement el && el.DataContext is T match)
                return match;
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private record PointChipDragPayload(GameSessionViewModel SourceVm, int SourceIndex);

    private bool _forceClose;

    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            // Only hide to tray if exit behavior is MinimizeToTray
            if (_mainVm.Settings.ExitBehavior == ExitBehavior.MinimizeToTray)
                Hide();
        }
        base.OnStateChanged(e);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        var exitBehavior = _mainVm.Settings.ExitBehavior;

        // MinimizeToTray: hide instead of close (unless force-closing from tray menu)
        if (exitBehavior == ExitBehavior.MinimizeToTray && !_forceClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        // If tasks are running, show warning
        if (_mainVm.HasAnyRunning && !_forceClose)
        {
            var result = MessageBox.Show(
                Strings.ConfirmExitMsg,
                Strings.ConfirmExitTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            _mainVm.StopAllCommand.Execute(null);
        }

        _hotkeyService?.UnregisterAll();
        _mainVm.SaveSettings();
        base.OnClosing(e);
    }

    private void OnResetAllStats(object sender, RoutedEventArgs e)
    {
        _dashboardVm.ResetDashboardCommand.Execute(null);
    }

    /// <summary>
    /// Called from tray menu "Exit" to force-close regardless of exit behavior.
    /// </summary>
    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }
}
