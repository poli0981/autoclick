using AutoClick.UI.ViewModels;

namespace AutoClick.UI.Views.Pages;

public partial class AboutPage
{
    public AboutPage(AboutViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
