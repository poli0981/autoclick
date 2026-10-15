using AutoClick.UI.ViewModels;

namespace AutoClick.UI.Views.Pages;

public partial class QueuePage
{
    public QueuePage(QueueViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
