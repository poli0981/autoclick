using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using AutoClick.Core.Abstractions;
using AutoClick.Core.Models;
using AutoClick.Core.Settings;
using AutoClick.UI.Localization;
using AutoClick.UI.ViewModels;
using AutoClick.UI.Views;
using AutoClick.UI.Views.Dialogs;
using AutoClick.UI.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using Wpf.Ui.Appearance;
using IconElement = Wpf.Ui.Controls.IconElement;

namespace AutoClick.UI.Services;

/// <summary>
/// <c>--smoke-test</c> (CI and release builds): opens every page in every theme and language,
/// the dialogs, the notices and a heatmap overlay, and checks that the text of the pages and the
/// charts follows the theme. Exits with 0, or 1 on any exception, data-binding error or wrong
/// text color. Runs with a separate data folder, never the user's.
/// </summary>
public sealed class SmokeTest(
    MainWindow window,
    MainWindowViewModel mainViewModel,
    ThemeService theme,
    QueueViewModel queue,
    DashboardViewModel dashboard,
    OverlayService overlays,
    IServiceProvider services,
    ILogger<SmokeTest> logger)
{
    private static readonly Type[] Pages = [typeof(QueuePage), typeof(DashboardPage), typeof(SettingsPage), typeof(AboutPage)];

    // Starts in Dark like a fresh start with Windows in dark mode (2.0.0 drew page text black there).
    private static readonly AppTheme[] Themes = [AppTheme.Dark, AppTheme.Light, AppTheme.Dark, AppTheme.HighContrast, AppTheme.System];

    // Following Windows, SystemThemeWatcher applies a theme through WPF-UI, not through ThemeService.
    private static readonly ApplicationTheme[] WindowsThemes = [ApplicationTheme.Dark, ApplicationTheme.Light];

    private int _colorErrors;

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
            // As in AppCoordinator.Start, the theme is applied before the window is shown: a theme
            // change on a shown window hid the missing page Foreground of 2.0.0.
            Application.Current.MainWindow = window;
            new WindowInteropHelper(window).EnsureHandle();
            theme.Apply(Themes[0], window);
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
                if (appTheme != theme.Current)
                {
                    theme.Apply(appTheme, window);
                }

                foreach (var language in AppSettings.SupportedLanguages)
                {
                    LocalizationService.Instance.SetLanguage(language);
                    foreach (var page in Pages)
                    {
                        window.Navigate(page);
                        await RenderAsync();
                        views++;

                        // Colors do not depend on the language.
                        if (language == AppSettings.SupportedLanguages[0])
                        {
                            CheckPageColors((Page)services.GetRequiredService(page), appTheme.ToString());
                        }
                    }
                }

                CheckChartColors(appTheme.ToString());
            }

            foreach (var windowsTheme in WindowsThemes)
            {
                ApplicationThemeManager.Apply(windowsTheme);
                await RenderAsync();
                CheckChartColors($"Windows {windowsTheme}");
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

            exitCode = bindingErrors.Errors.Count == 0 && _colorErrors == 0 ? 0 : 1;
            logger.LogInformation("Smoke test finished: {Views} page views, {Errors} binding error(s), {ColorErrors} color error(s)",
                views, bindingErrors.Errors.Count, _colorErrors);
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

    /// <summary>
    /// Text and icons without a color of their own take the color of the page. WPF-UI's implicit
    /// Page style never reaches page subclasses and the navigation frame does not pass the window's
    /// color on, so each page sets a themed Foreground; without it this text was black (2.0.0).
    /// </summary>
    private void CheckPageColors(Page page, string themeName)
    {
        var wrong = new List<string>();
        if (ForegroundSource(page) != BaseValueSource.Local)
        {
            wrong.Add("the page sets no Foreground");
        }

        var expected = (Application.Current.TryFindResource("TextFillColorPrimaryBrush") as SolidColorBrush)?.Color;
        foreach (var element in Descendants(page).OfType<FrameworkElement>())
        {
            if (element is not (TextBlock or IconElement) || !element.IsVisible)
            {
                continue;
            }

            var source = ForegroundSource(element);
            var color = (element.GetValue(TextElement.ForegroundProperty) as SolidColorBrush)?.Color;
            if (source == BaseValueSource.Default
                || (source == BaseValueSource.Inherited && ForegroundProvider(element) == page && color != expected))
            {
                wrong.Add($"{(element as TextBlock)?.Text ?? element.GetType().Name} ({color})");
            }
        }

        if (wrong.Count > 0)
        {
            _colorErrors++;
            logger.LogError("Text without the theme color on {Page} ({Theme}), {Count} element(s): {Examples}",
                page.GetType().Name, themeName, wrong.Count, string.Join("; ", wrong.Take(5)));
        }
    }

    /// <summary>The dashboard paints chart text with theme colors read on <see cref="ThemeService.ThemeChanged"/>.</summary>
    private void CheckChartColors(string themeName)
    {
        if (Application.Current.TryFindResource("TextFillColorSecondaryBrush") is not SolidColorBrush { Color: var expected })
        {
            return;
        }

        var actual = dashboard.LegendTextPaint.Color;
        if (actual != new SKColor(expected.R, expected.G, expected.B, expected.A))
        {
            _colorErrors++;
            logger.LogError("Chart text color not updated ({Theme}): {Actual}, expected {Expected}", themeName, actual, expected);
        }
    }

    // The nearest ancestor that has its own Foreground, the one the element inherits from.
    private static DependencyObject? ForegroundProvider(DependencyObject element)
    {
        var current = InheritanceParent(element);
        while (current is not null && ForegroundSource(current) == BaseValueSource.Inherited)
        {
            current = InheritanceParent(current);
        }

        return current;
    }

    // WPF inherits along the logical parent when there is one, otherwise along the visual parent.
    private static DependencyObject? InheritanceParent(DependencyObject element) =>
        LogicalTreeHelper.GetParent(element) ?? (element is Visual ? VisualTreeHelper.GetParent(element) : null);

    private static BaseValueSource ForegroundSource(DependencyObject element) =>
        DependencyPropertyHelper.GetValueSource(element, TextElement.ForegroundProperty).BaseValueSource;

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        var pending = new Stack<DependencyObject>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(current); i++)
            {
                var child = VisualTreeHelper.GetChild(current, i);
                pending.Push(child);
                yield return child;
            }
        }
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
