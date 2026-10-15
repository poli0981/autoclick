using System.ComponentModel;
using AutoClick.UI.Services;
using AutoClick.UI.ViewModels;
using AutoClick.UI.Views.Pages;
using Wpf.Ui;

namespace AutoClick.UI.Views;

public partial class MainWindow
{
    private readonly AppLifetime _lifetime;

    public MainWindow(MainWindowViewModel viewModel, IServiceProvider services, IContentDialogService dialogs, ISnackbarService snackbar, AppLifetime lifetime)
    {
        _lifetime = lifetime;
        DataContext = viewModel;
        InitializeComponent();
        dialogs.SetDialogHost(RootContentDialog);
        snackbar.SetSnackbarPresenter(SnackbarPresenter);
        RootNavigation.SetServiceProvider(services);
        Loaded += (_, _) => Navigate(typeof(QueuePage));
    }

    /// <summary>The user closed the window (the exit behavior setting decides what happens).</summary>
    public event EventHandler? CloseRequested;

    public void Navigate(Type pageType) => RootNavigation.Navigate(pageType);

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_lifetime.IsExiting)
        {
            e.Cancel = true;
            CloseRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        base.OnClosing(e);
    }
}
