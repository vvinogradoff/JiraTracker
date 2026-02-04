using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using UpworkJiraTracker.Helper;

namespace UpworkJiraTracker.ViewModel;

public class JiraCredentialsOverlayViewModel : INotifyPropertyChanged
{
    private string _clientId = string.Empty;
    private string _clientSecret = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<(string ClientId, string ClientSecret)>? ConnectRequested;
    public event EventHandler? CancelRequested;

    public ICommand ConnectCommand { get; }
    public ICommand CancelCommand { get; }

    public string ClientId
    {
        get => _clientId;
        set
        {
            if (_clientId != value)
            {
                _clientId = value;
                OnPropertyChanged();
            }
        }
    }

    public string ClientSecret
    {
        get => _clientSecret;
        set
        {
            if (_clientSecret != value)
            {
                _clientSecret = value;
                OnPropertyChanged();
            }
        }
    }

    public JiraCredentialsOverlayViewModel()
    {
        ConnectCommand = new RelayCommand(_ => Connect());
        CancelCommand = new RelayCommand(_ => CancelRequested?.Invoke(this, EventArgs.Empty));
    }

    private void Connect()
    {
        var clientId = ClientId?.Trim() ?? "";
        var clientSecret = ClientSecret?.Trim() ?? "";

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return;
        }

        ConnectRequested?.Invoke(this, (clientId, clientSecret));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
