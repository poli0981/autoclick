using System.Collections.ObjectModel;
using AutoClick.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoClick.UI.ViewModels;

/// <summary>"Add game": the windows that can be clicked, with a search box.</summary>
public sealed partial class GamePickerViewModel : ObservableObject
{
    private readonly Func<IReadOnlyList<WindowInfo>> _source;
    private IReadOnlyList<WindowInfo> _all = [];

    public GamePickerViewModel(Func<IReadOnlyList<WindowInfo>> source)
    {
        _source = source;
        Refresh();
    }

    public ObservableCollection<WindowInfo> Windows { get; } = [];

    [ObservableProperty]
    public partial string Filter { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    public partial WindowInfo? Selected { get; set; }

    public bool HasSelection => Selected is not null;

    public bool IsEmpty => Windows.Count == 0;

    [RelayCommand]
    public void Refresh()
    {
        var selected = Selected?.Handle;
        _all = _source();
        ApplyFilter();
        Selected = Windows.FirstOrDefault(w => w.Handle == selected);
    }

    partial void OnFilterChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filter = Filter.Trim();
        Windows.Clear();
        foreach (var window in _all.Where(w => filter.Length == 0
            || w.ProcessName.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || w.Title.Contains(filter, StringComparison.OrdinalIgnoreCase)))
        {
            Windows.Add(window);
        }

        OnPropertyChanged(nameof(IsEmpty));
    }
}
