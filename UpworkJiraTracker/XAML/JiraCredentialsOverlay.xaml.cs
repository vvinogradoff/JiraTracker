using System.Windows;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace UpworkJiraTracker.XAML;

public partial class JiraCredentialsOverlay : WpfUserControl
{
    public event EventHandler<(string ClientId, string ClientSecret)>? CredentialsSubmitted;
    public event EventHandler? Cancelled;

    public JiraCredentialsOverlay()
    {
        InitializeComponent();
    }

    public void SetCredentials(string clientId, string clientSecret)
    {
        ClientIdTextBox.Text = clientId;
        ClientSecretTextBox.Text = clientSecret;
    }

    private void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        var clientId = ClientIdTextBox.Text?.Trim() ?? "";
        var clientSecret = ClientSecretTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return;
        }

        CredentialsSubmitted?.Invoke(this, (clientId, clientSecret));
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}
