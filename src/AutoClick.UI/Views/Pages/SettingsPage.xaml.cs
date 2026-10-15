using AutoClick.UI.ViewModels;

namespace AutoClick.UI.Views.Pages;

public partial class SettingsPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
