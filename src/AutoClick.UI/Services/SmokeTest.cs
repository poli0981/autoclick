using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using AutoClick.Core.Abstractions;
using AutoClick.Core.Models;
using AutoClick.Core.Settings;
using AutoClick.UI.Localization;
using AutoClick.UI.ViewModels;
using AutoClick.UI.Views;
using AutoClick.UI.Views.Dialogs;
using AutoClick.UI.Views.Pages;
using Microsoft.Extensions.Logging;

namespace AutoClick.UI.Services;

/// <summary>
/// <c>--smoke-test</c> (CI and release builds): opens every page in every theme and language,
/// the dialogs, the notices and a heatmap overlay, then exits with 0, or 1 on any exception or
/// data-binding error. Runs with a separate data folder, never the user's.
/// </summary>
public sealed class SmokeTest(
    MainWindow window,
    MainWindowViewModel mainViewModel,
    ThemeService theme,
    QueueViewModel queue,
    OverlayService overlays,
    ILogger<SmokeTest> logger)
{
    private static readonly Type[] Pages = [typeof(QueuePage), typeof(DashboardPage), typeof(SettingsPage), typeof(AboutPage)];
    private static readonly AppTheme[] Themes = [AppTheme.Light, AppTheme.Dark, AppTheme.HighContrast, AppTheme.System];

    public async Task RunAsync()
    {
        var bindingErrors = new BindingErrorListener();
        PresentationTraceSources.Refresh();
        PresentationTraceSources.DataBindingSource.Listeners.Add(bindingErrors);
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;

        var exitCode = 1;
        var views = 0;
        try
        {
            Application.Current.MainWindow = window;
            window.Show();
            var handle = new WindowInteropHelper(window).Handle;

            // A game card with both kinds of steps, using AutoClick's own window as the "game".
            var game = queue.AddWindow(new WindowInfo(handle, Environment.ProcessId, "AutoClick.UI", "Smoke test", Environment.ProcessPath ?? string.Empty, ElevationState.SameOrLower));
            game.Points.Add(new ClickPoint { X = 40, Y = 60, ReferenceColor = 0x00336699 });
            game.Points.Add(new ClickPoint { ClickType = ClickType.Keystroke, VirtualKeyCode = 0x20 });
            game.IsCustomMode = true;
            mainViewModel.ShowElevationRequired = true;
            mainViewModel.ShowUpdateReady = true;
            mainViewModel.ShowSettingsRecovered = true;

            foreach (var appTheme in Themes)
            {
                theme.Apply(appTheme, window);
                foreach (var language in AppSettings.SupportedLanguages)
                {
                    LocalizationService.Instance.SetLanguage(language);
                    foreach (var page in Pages)
                    {
                        window.Navigate(page);
                        await RenderAsync();
                        views++;
                    }
                }
            }

            await ShowStandaloneAsync(new GamePickerView(new GamePickerViewModel(() =>
                [new WindowInfo(handle, Environment.ProcessId, "game.exe", "Game", @"C:\Games\game.exe", ElevationState.Higher)])));
            await ShowStandaloneAsync(new HotkeyCaptureView());

            game.Stats.RecordClick(40, 60);
            game.Stats.RecordClick(40, 60);
            game.Stats.RecordClick(80, 20);
            overlays.ToggleHeatmap(game.Id, handle, Environment.ProcessId, game.Stats);
            await RenderAsync();
            overlays.CloseAll();

            game.Remove();
            await RenderAsync();

            foreach (var error in bindingErrors.Errors)
            {
                logger.LogError("Binding error: {Error}", error);
            }

            exitCode = bindingErrors.Errors.Count == 0 ? 0 : 1;
            logger.LogInformation("Smoke test finished: {Views} page views, {Errors} binding error(s)", views, bindingErrors.Errors.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Smoke test failed after {Views} page views", views);
        }
        finally
        {
            PresentationTraceSources.DataBindingSource.Listeners.Remove(bindingErrors);
            Application.Current.Shutdown(exitCode);
        }
    }

    private static async Task RenderAsync()
    {
        // Let navigation, layout, data binding and rendering finish.
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        await Task.Delay(40);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    }

    private static async Task ShowStandaloneAsync(FrameworkElement content)
    {
        var host = new Window
        {
            Content = content,
            Width = 720,
            Height = 560,
            ShowActivated = false,
            ShowInTaskbar = false,
        };
        host.Show();
        await RenderAsync();
        host.Close();
    }

    private sealed class BindingErrorListener : TraceListener
    {
        public List<string> Errors { get; } = [];

        public override void Write(string? message)
        {
        }

        public override void WriteLine(string? message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                Errors.Add(message);
            }
        }
    }
}
