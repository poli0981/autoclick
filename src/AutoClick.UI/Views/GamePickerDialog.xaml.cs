using System.Windows;
using System.Windows.Input;
using AutoClick.Core.Interfaces;
using AutoClick.Core.Models;

namespace AutoClick.UI.Views;

public partial class GamePickerDialog : Window
{
    private List<GameWindowInfo> _allWindows;
    private readonly IGameDetector? _gameDetector;
    public GameWindowInfo? SelectedWindow { get; private set; }

    public GamePickerDialog(List<GameWindowInfo> windows, IGameDetector? gameDetector = null)
    {
        InitializeComponent();
        _allWindows = windows;
        _gameDetector = gameDetector;
        PopulateList(windows);
    }

    private void PopulateList(List<GameWindowInfo> windows)
    {
        WindowList.Items.Clear();
        foreach (var w in windows)
        {
            WindowList.Items.Add(new WindowListItem
            {
                Info = w,
                DisplayText = $"{w.ProcessName} - {w.Title}"
            });
        }
    }

    private void OnSearchTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var query = SearchBox.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(query))
        {
            PopulateList(_allWindows);
            return;
        }

        var filtered = _allWindows.Where(w =>
            w.ProcessName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            w.Title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        PopulateList(filtered);
    }

    private void OnSelect(object sender, RoutedEventArgs e)
    {
        if (WindowList.SelectedItem is WindowListItem item)
        {
            SelectedWindow = item.Info;
            DialogResult = true;
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        if (_gameDetector != null)
        {
            _allWindows = _gameDetector.GetRunningWindows();
        }

        var query = SearchBox.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(query))
            PopulateList(_allWindows);
        else
        {
            var filtered = _allWindows.Where(w =>
                w.ProcessName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                w.Title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
            PopulateList(filtered);
        }
    }

    private void OnWindowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OnSelect(sender, e);
    }

    private class WindowListItem
    {
        public GameWindowInfo Info { get; set; } = null!;
        public string DisplayText { get; set; } = string.Empty;
        public override string ToString() => DisplayText;
    }
}
