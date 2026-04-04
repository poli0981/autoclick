using System.Windows;

namespace AutoClick.UI.Views;

public partial class ProfileNameDialog : Window
{
    public string ProfileName => NameBox.Text.Trim();

    public ProfileNameDialog(string? existingName = null)
    {
        InitializeComponent();
        if (existingName != null)
            NameBox.Text = existingName;
    }

    private void NameBox_Loaded(object sender, RoutedEventArgs e)
    {
        NameBox.Focus();
        NameBox.SelectAll();
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(NameBox.Text))
            DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
