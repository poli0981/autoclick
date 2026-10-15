using System.Windows.Input;
using AutoClick.UI.ViewModels;

namespace AutoClick.UI.Views.Dialogs;

public partial class GamePickerView
{
    public GamePickerView(GamePickerViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        Loaded += (_, _) => SearchBox.Focus();
    }

    /// <summary>Raised when a window is double-clicked (accept it).</summary>
    public event EventHandler? Accepted;

    private void OnListDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (WindowList.SelectedItem is not null)
        {
            Accepted?.Invoke(this, EventArgs.Empty);
        }
    }
}
