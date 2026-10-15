using System.Windows;
using System.Windows.Controls;
using AutoClick.Core.Abstractions;
using AutoClick.Core.Hotkeys;
using AutoClick.UI.Localization;
using AutoClick.UI.ViewModels;
using AutoClick.UI.Views.Dialogs;
using Microsoft.Win32;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace AutoClick.UI.Services;

public enum ToastKind
{
    Info,
    Success,
    Warning,
    Error,
}

/// <summary>Fluent dialogs, toasts (snackbar) and file pickers for view models.</summary>
public sealed class DialogService(IContentDialogService dialogs, ISnackbarService snackbar)
{
    public async Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = Wrap(message),
            PrimaryButtonText = confirmText,
            CloseButtonText = L.Get("Common_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
        };
        return await dialogs.ShowAsync(dialog, CancellationToken.None) == ContentDialogResult.Primary;
    }

    public async Task AlertAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = Wrap(message),
            CloseButtonText = L.Get("Common_Ok"),
        };
        await dialogs.ShowAsync(dialog, CancellationToken.None);
    }

    /// <summary>Asks for a line of text; returns null when cancelled.</summary>
    public async Task<string?> PromptAsync(string title, string label, string initialValue = "")
    {
        var box = new Wpf.Ui.Controls.TextBox { Text = initialValue, MinWidth = 320, PlaceholderText = label };
        var panel = new StackPanel();
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 8) });
        panel.Children.Add(box);
        var dialog = new ContentDialog
        {
            Title = title,
            Content = panel,
            PrimaryButtonText = L.Get("Common_Ok"),
            CloseButtonText = L.Get("Common_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
        };
        dialog.Loaded += (_, _) =>
        {
            box.Focus();
            box.SelectAll();
        };

        var result = await dialogs.ShowAsync(dialog, CancellationToken.None);
        var text = box.Text?.Trim();
        return result == ContentDialogResult.Primary && !string.IsNullOrEmpty(text) ? text : null;
    }

    /// <summary>Shows custom content; returns true when the primary button was pressed.</summary>
    public async Task<bool> ShowAsync(string title, object content, string primaryText)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = primaryText,
            CloseButtonText = L.Get("Common_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
        };
        return await dialogs.ShowAsync(dialog, CancellationToken.None) == ContentDialogResult.Primary;
    }

    /// <summary>"Add game": lets the user pick one of the windows from <paramref name="source"/>.</summary>
    public async Task<WindowInfo?> PickGameAsync(Func<IReadOnlyList<WindowInfo>> source)
    {
        var viewModel = new GamePickerViewModel(source);
        var view = new GamePickerView(viewModel);
        var dialog = new ContentDialog
        {
            Title = L.Get("Picker_GameTitle"),
            Content = view,
            PrimaryButtonText = L.Get("Picker_Select"),
            CloseButtonText = L.Get("Common_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            DialogMaxWidth = 760,
        };
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(GamePickerViewModel.HasSelection))
            {
                dialog.IsPrimaryButtonEnabled = viewModel.HasSelection;
            }
        };
        view.Accepted += (_, _) => dialog.Hide(ContentDialogResult.Primary);

        var result = await dialogs.ShowAsync(dialog, CancellationToken.None);
        return result == ContentDialogResult.Primary ? viewModel.Selected : null;
    }

    /// <summary>Records a key combination; null when cancelled.</summary>
    public async Task<HotkeyGesture?> CaptureHotkeyAsync(string actionName)
    {
        var view = new HotkeyCaptureView();
        var dialog = new ContentDialog
        {
            Title = L.Format("Hotkey_RecordTitle", actionName),
            Content = view,
            PrimaryButtonText = L.Get("Common_Save"),
            CloseButtonText = L.Get("Common_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
        };
        dialog.PreviewKeyDown += (_, e) => view.HandleKey(e);
        view.GestureChanged += (_, _) => dialog.IsPrimaryButtonEnabled = view.Gesture is not null;
        dialog.Loaded += (_, _) => view.Focus();

        var result = await dialogs.ShowAsync(dialog, CancellationToken.None);
        return result == ContentDialogResult.Primary ? view.Gesture : null;
    }

    public void Toast(string title, string message, ToastKind kind = ToastKind.Info)
    {
        var (appearance, symbol) = kind switch
        {
            ToastKind.Success => (ControlAppearance.Success, SymbolRegular.CheckmarkCircle24),
            ToastKind.Warning => (ControlAppearance.Caution, SymbolRegular.Warning24),
            ToastKind.Error => (ControlAppearance.Danger, SymbolRegular.ErrorCircle24),
            _ => (ControlAppearance.Secondary, SymbolRegular.Info24),
        };

        if (snackbar.GetSnackbarPresenter() is not null)
        {
            snackbar.Show(title, message, appearance, new SymbolIcon(symbol), TimeSpan.FromSeconds(kind == ToastKind.Error ? 8 : 4));
        }
    }

    public static string? PickOpenFile(string filter)
    {
        var dialog = new OpenFileDialog { Filter = filter, CheckFileExists = true };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public static string? PickSaveFile(string filter, string fileName)
    {
        var dialog = new SaveFileDialog { Filter = filter, FileName = fileName, AddExtension = true, OverwritePrompt = true };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static System.Windows.Controls.TextBlock Wrap(string message) =>
        new() { Text = message, TextWrapping = TextWrapping.Wrap, MaxWidth = 460 };
}
