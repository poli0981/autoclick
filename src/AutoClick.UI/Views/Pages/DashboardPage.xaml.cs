using AutoClick.UI.ViewModels;

namespace AutoClick.UI.Views.Pages;

public partial class DashboardPage
{
    public DashboardPage(DashboardViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
