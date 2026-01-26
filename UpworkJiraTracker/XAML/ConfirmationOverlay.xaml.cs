using System.Windows;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace UpworkJiraTracker.XAML;

public partial class ConfirmationOverlay : WpfUserControl
{
    public event EventHandler? Confirmed;
    public event EventHandler? Cancelled;

    public ConfirmationOverlay()
    {
        InitializeComponent();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Confirmed?.Invoke(this, EventArgs.Empty);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}
