using System.Windows;
using WpfUserControl = System.Windows.Controls.UserControl;
using UpworkJiraTracker.ViewModel;

namespace UpworkJiraTracker.XAML;

public partial class JiraCredentialsOverlay : WpfUserControl
{
    private readonly JiraCredentialsOverlayViewModel _viewModel;

    public event EventHandler<(string ClientId, string ClientSecret)>? CredentialsSubmitted;
    public event EventHandler? Cancelled;

    public JiraCredentialsOverlay()
    {
        InitializeComponent();

        _viewModel = new JiraCredentialsOverlayViewModel();
        DataContext = _viewModel;

        _viewModel.ConnectRequested += (s, credentials) => CredentialsSubmitted?.Invoke(this, credentials);
        _viewModel.CancelRequested += (s, e) => Cancelled?.Invoke(this, EventArgs.Empty);
    }

    public void SetCredentials(string clientId, string clientSecret)
    {
        _viewModel.ClientId = clientId;
        _viewModel.ClientSecret = clientSecret;
    }
}
