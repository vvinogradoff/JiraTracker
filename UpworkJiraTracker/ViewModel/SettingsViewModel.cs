using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using UpworkJiraTracker.Helper;
using UpworkJiraTracker.Model;

using WpfColor = System.Windows.Media.Color;

namespace UpworkJiraTracker.ViewModel;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly MainWindowViewModel _mainViewModel;
    private WpfColor? _customBackgroundColor;
    private string _jiraStatusText = "Not connected";
    private bool _isJiraConnected = false;
    private bool _isDeelConnected = false;
    private UpworkState _upworkState = UpworkState.NoProcess;
    private double _mainWindowWidth = 180;
    private double _mainWindowHeight = 48;
    private string _logDirectory = ".";
    private int _topmostEnforcementIntervalSeconds = 5;
    private bool _showDeelBrowser = true;
    private int _pauseOnInactivityMinutes = 0;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? PickColorRequested;
    public event EventHandler? CloseRequested;
    public event EventHandler? AddTimezoneRequested;
    public event EventHandler? ConnectJiraRequested;
    public event EventHandler? BrowseLogDirectoryRequested;
    public event EventHandler? OpenTimeLogRequested;
    public event EventHandler? MinimizeRequested;

    public ICommand PickColorCommand { get; }
    public ICommand ResetColorCommand { get; }
    public ICommand TransparentColorCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand MinimizeCommand { get; }
    public ICommand AddTimezoneCommand { get; }
    public ICommand RemoveTimezoneCommand { get; }
    public ICommand ConnectJiraCommand { get; }
    public ICommand DisconnectJiraCommand { get; }
    public ICommand ToggleJiraConnectionCommand { get; }
    public ICommand BrowseLogDirectoryCommand { get; }
    public ICommand OpenTimeLogCommand { get; }

    public ObservableCollection<TimezoneEntry> Timezones { get; } = new();

	[Obsolete("For design-time only", true)]
	public SettingsViewModel() { }

    public SettingsViewModel(MainWindowViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;

        PickColorCommand = new RelayCommand(_ => PickColorRequested?.Invoke(this, EventArgs.Empty));
        ResetColorCommand = new RelayCommand(_ => ResetColor());
        TransparentColorCommand = new RelayCommand(_ => SetTransparentColor());
        CloseCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, EventArgs.Empty));
        MinimizeCommand = new RelayCommand(_ => MinimizeRequested?.Invoke(this, EventArgs.Empty));
        AddTimezoneCommand = new RelayCommand(_ => AddTimezoneRequested?.Invoke(this, EventArgs.Empty));
        RemoveTimezoneCommand = new RelayCommand(param =>
        {
            if (param is TimezoneEntry entry)
            {
                Timezones.Remove(entry);
            }
        });
        ConnectJiraCommand = new RelayCommand(_ => ConnectJiraRequested?.Invoke(this, EventArgs.Empty));
        DisconnectJiraCommand = new RelayCommand(async _ => await DisconnectJiraAsync());
        ToggleJiraConnectionCommand = new RelayCommand(async _ => await ToggleJiraConnectionAsync());
        BrowseLogDirectoryCommand = new RelayCommand(_ => BrowseLogDirectoryRequested?.Invoke(this, EventArgs.Empty));
        OpenTimeLogCommand = new RelayCommand(_ => OpenTimeLogRequested?.Invoke(this, EventArgs.Empty));
    }

    private void ResetColor()
    {
        CustomBackgroundColor = null;
    }

    private void SetTransparentColor()
    {
        CustomBackgroundColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 0);
    }

    private async Task DisconnectJiraAsync()
    {
        try
        {
            JiraStatusText = "Disconnecting...";
            await _mainViewModel.JiraService.DisconnectAsync();
        }
        catch (Exception ex)
        {
            JiraStatusText = $"Error: {ex.Message}";
        }
    }

    private async Task ToggleJiraConnectionAsync()
    {
        if (IsJiraConnected)
        {
            await DisconnectJiraAsync();
        }
        else
        {
            ConnectJiraRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public string JiraStatusText
    {
        get => _jiraStatusText;
        set
        {
            if (_jiraStatusText != value)
            {
                _jiraStatusText = value;
                OnPropertyChanged();
            }
        }
    }

    public UpworkState UpworkState
	{
        get => _upworkState;
        set
        {
            if (_upworkState != value)
            {
                _upworkState = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsJiraConnected
    {
        get => _isJiraConnected;
        set
        {
            if (_isJiraConnected != value)
            {
                _isJiraConnected = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsDeelConnected
    {
        get => _isDeelConnected;
        set
        {
            if (_isDeelConnected != value)
            {
                _isDeelConnected = value;
                OnPropertyChanged();
            }
        }
    }

	public double MainWindowWidth
    {
        get => _mainWindowWidth;
        set
        {
            if (_mainWindowWidth != value)
            {
                _mainWindowWidth = value;
                OnPropertyChanged();
            }
        }
    }

    public double MainWindowHeight
    {
        get => _mainWindowHeight;
        set
        {
            if (_mainWindowHeight != value)
            {
                _mainWindowHeight = value;
                OnPropertyChanged();
            }
        }
    }

    public WpfColor? CustomBackgroundColor
    {
        get => _customBackgroundColor;
        set
        {
            if (_customBackgroundColor != value)
            {
                _customBackgroundColor = value;
                OnPropertyChanged();
            }
        }
    }

    public string LogDirectory
    {
        get => _logDirectory;
        set
        {
            if (_logDirectory != value)
            {
                _logDirectory = value;
                OnPropertyChanged();
            }
        }
    }

    public int TopmostEnforcementIntervalSeconds
    {
        get => _topmostEnforcementIntervalSeconds;
        set
        {
            if (_topmostEnforcementIntervalSeconds != value)
            {
                _topmostEnforcementIntervalSeconds = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ShowDeelBrowser
    {
        get => _showDeelBrowser;
        set
        {
            if (_showDeelBrowser != value)
            {
                _showDeelBrowser = value;
                OnPropertyChanged();
            }
        }
    }

    public int PauseOnInactivityMinutes
    {
        get => _pauseOnInactivityMinutes;
        set
        {
            if (_pauseOnInactivityMinutes != value)
            {
                _pauseOnInactivityMinutes = value;
                OnPropertyChanged();
            }
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
