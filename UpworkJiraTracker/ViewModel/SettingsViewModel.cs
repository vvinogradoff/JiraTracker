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
    private WpfColor? _customBackgroundColor;
    private string _jiraStatusText = "Not connected";
    private bool _isJiraConnected = false;
    private UpworkState _upworkState = UpworkState.NoProcess;
    private double _mainWindowWidth = 180;
    private double _mainWindowHeight = 48;
    private string _logDirectory = ".";
    private int _topmostEnforcementIntervalSeconds = 5;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? PickColorRequested;
    public event EventHandler? ResetColorRequested;
    public event EventHandler? CloseRequested;
    public event EventHandler? AddTimezoneRequested;
    public event EventHandler<TimezoneEntry>? RemoveTimezoneRequested;
    public event EventHandler? ConnectJiraRequested;
    public event EventHandler? DisconnectJiraRequested;
    public event EventHandler? BrowseLogDirectoryRequested;

    public ICommand PickColorCommand { get; }
    public ICommand ResetColorCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand AddTimezoneCommand { get; }
    public ICommand RemoveTimezoneCommand { get; }
    public ICommand ConnectJiraCommand { get; }
    public ICommand DisconnectJiraCommand { get; }
    public ICommand BrowseLogDirectoryCommand { get; }

    public ObservableCollection<TimezoneEntry> Timezones { get; } = new();

    public SettingsViewModel()
    {
        PickColorCommand = new RelayCommand(_ => PickColorRequested?.Invoke(this, EventArgs.Empty));
        ResetColorCommand = new RelayCommand(_ => ResetColorRequested?.Invoke(this, EventArgs.Empty));
        CloseCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, EventArgs.Empty));
        AddTimezoneCommand = new RelayCommand(_ => AddTimezoneRequested?.Invoke(this, EventArgs.Empty));
        RemoveTimezoneCommand = new RelayCommand(param =>
        {
            if (param is TimezoneEntry entry)
            {
                RemoveTimezoneRequested?.Invoke(this, entry);
            }
        });
        ConnectJiraCommand = new RelayCommand(_ => ConnectJiraRequested?.Invoke(this, EventArgs.Empty));
        DisconnectJiraCommand = new RelayCommand(_ => DisconnectJiraRequested?.Invoke(this, EventArgs.Empty));
        BrowseLogDirectoryCommand = new RelayCommand(_ => BrowseLogDirectoryRequested?.Invoke(this, EventArgs.Empty));
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

    /// <summary>
    /// Async initialization that checks Upwork state.
    /// Call this from Window.Loaded event.
    /// </summary>
    public async Task InitializeUpworkStateAsync(Service.UpworkIntegrationFlaUI upworkIntegration)
    {
        // Check if Upwork process is available
        if (!upworkIntegration.IsUpworkAvailable())
        {
            UpworkState = UpworkState.NoProcess;
            return;
        }

        // Process is available, check if we can automate it
        var weeklyTotal = await upworkIntegration.ReadWeeklyTotal();
        if (weeklyTotal.HasValue)
        {
            UpworkState = UpworkState.FullyAutomated;
        }
        else
        {
            UpworkState = UpworkState.ProcessFoundButCannotAutomate;
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
