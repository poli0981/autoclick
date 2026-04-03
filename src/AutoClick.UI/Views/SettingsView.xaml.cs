using AutoClick.UI.ViewModels;

namespace AutoClick.UI.Views;

public partial class SettingsView : System.Windows.Controls.UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        Focusable = true;
    }

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && vm.CapturingHotkeyFor != null)
        {
            e.Handled = true;

            // ESC cancels
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                vm.CancelCapture();
                return;
            }

            // Ignore modifier-only keys
            var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
            if (key == System.Windows.Input.Key.LeftShift || key == System.Windows.Input.Key.RightShift ||
                key == System.Windows.Input.Key.LeftCtrl || key == System.Windows.Input.Key.RightCtrl ||
                key == System.Windows.Input.Key.LeftAlt || key == System.Windows.Input.Key.RightAlt ||
                key == System.Windows.Input.Key.LWin || key == System.Windows.Input.Key.RWin)
                return;

            // Build key combo string
            var parts = new System.Collections.Generic.List<string>();
            if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
                parts.Add("Ctrl");
            if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))
                parts.Add("Alt");
            if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
                parts.Add("Shift");
            parts.Add(key.ToString());

            vm.HandleCapturedKey(string.Join("+", parts));
        }
    }
}
