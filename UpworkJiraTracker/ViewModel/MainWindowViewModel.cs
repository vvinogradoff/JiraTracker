using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using WpfColor = System.Windows.Media.Color;
using WpfBrush = System.Windows.Media.Brush;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfColors = System.Windows.Media.Colors;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using UpworkJiraTracker.Service;
using UpworkJiraTracker.Model;
using UpworkJiraTracker.Helper;

namespace UpworkJiraTracker.ViewModel;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public class TimeDisplay
    {
        public string Text { get; set; } = string.Empty;
    }

    private readonly UpworkIntegrationFlaUI _upworkIntegration;
    private readonly UpworkWindowWatcherService _upworkWindowWatcher;
    private readonly JiraOAuthService _jiraService;
    private readonly JiraIssueCacheService _jiraCacheService;
    private readonly JiraIssuesService _jiraIssuesService;
    private readonly TimeTrackingService _timeTrackingService;
    private readonly WindowSettingsService _settingsService;
    private readonly DispatcherTimer _timeUpdateTimer;
    private readonly DispatcherTimer _upworkReadTimer;
    private bool _upworkReadTimerPending = false;

    private string _displayText = "Upwork Memo";
    private bool _isAutocompleteActive = false;
    private bool _isPlaying = false;
    private bool _isProcessing = false;
    private WpfColor? _customBackgroundColor = null;
    private WpfBrush _backgroundBrush = new WpfSolidColorBrush(WpfColors.Black);
    private WpfBrush _foregroundBrush = new WpfSolidColorBrush(WpfColors.White);
    private WpfBrush _iconFillBrush = new WpfSolidColorBrush(WpfColors.Transparent);
    private WpfBrush _iconStrokeBrush = new WpfSolidColorBrush(WpfColors.White);
    private double _iconStrokeThickness = 2;
    private string _timerTime = "0:00";
    private string _playPauseIconData = Constants.Icons.StopIcon;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? AutocompleteActivated;

    public ICommand PlayPauseCommand { get; }

    public ObservableCollection<TimezoneEntry> Timezones { get; } = new();
    public ObservableCollection<TimeDisplay> TimeDisplays { get; } = new();

    public string DisplayText
    {
        get => _displayText;
        set
        {
            if (_displayText != value)
            {
                _displayText = value;
                OnPropertyChanged();
                SaveLastMemo(value);
            }
        }
    }

    public bool IsAutocompleteActive
    {
        get => _isAutocompleteActive;
        set
        {
            if (_isAutocompleteActive != value)
            {
                _isAutocompleteActive = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (_isPlaying != value)
            {
                _isPlaying = value;
                OnPropertyChanged();
                UpdatePlayPauseIcon();
            }
        }
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        private set
        {
            if (_isProcessing != value)
            {
                _isProcessing = value;
                OnPropertyChanged();
                UpdatePlayPauseIcon();
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
                UpdateColors();
            }
        }
    }

    public WpfBrush BackgroundBrush
    {
        get => _backgroundBrush;
        private set
        {
            if (_backgroundBrush != value)
            {
                _backgroundBrush = value;
                OnPropertyChanged();
            }
        }
    }

    public WpfBrush ForegroundBrush
    {
        get => _foregroundBrush;
        private set
        {
            if (_foregroundBrush != value)
            {
                _foregroundBrush = value;
                OnPropertyChanged();
            }
        }
    }

    public string PlayPauseIconData
    {
        get => _playPauseIconData;
        private set
        {
            if (_playPauseIconData != value)
            {
                _playPauseIconData = value;
                OnPropertyChanged();
            }
        }
    }

    public WpfBrush IconFillBrush
    {
        get => _iconFillBrush;
        private set
        {
            if (_iconFillBrush != value)
            {
                _iconFillBrush = value;
                OnPropertyChanged();
            }
        }
    }

    public WpfBrush IconStrokeBrush
    {
        get => _iconStrokeBrush;
        private set
        {
            if (_iconStrokeBrush != value)
            {
                _iconStrokeBrush = value;
                OnPropertyChanged();
            }
        }
    }

    public double IconStrokeThickness
    {
        get => _iconStrokeThickness;
        private set
        {
            if (_iconStrokeThickness != value)
            {
                _iconStrokeThickness = value;
                OnPropertyChanged();
            }
        }
    }

    public string TimerTime
	{
        get => _timerTime;
        private set
        {
            if (_timerTime != value)
            {
				_timerTime = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsJiraAuthenticated => _jiraService.IsAuthenticated;

    public UpworkIntegrationFlaUI UpworkIntegration => _upworkIntegration;
    public JiraOAuthService JiraService => _jiraService;
    public JiraIssueCacheService JiraCacheService => _jiraCacheService;
    public JiraIssuesService JiraIssuesService => _jiraIssuesService;
    public TimeTrackingService TimeTrackingService => _timeTrackingService;
    public WindowSettingsService SettingsService => _settingsService;

    public MainWindowViewModel(UpworkIntegrationFlaUI upworkIntegration, JiraOAuthService jiraService, WindowSettingsService settingsService)
    {
        _upworkIntegration = upworkIntegration;
        _upworkWindowWatcher = new UpworkWindowWatcherService();
        _jiraService = jiraService;
        _settingsService = settingsService;
        _jiraCacheService = new JiraIssueCacheService(jiraService);
        _jiraIssuesService = new JiraIssuesService(jiraService, upworkIntegration, _jiraCacheService);
        _timeTrackingService = new TimeTrackingService(_jiraIssuesService);

        PlayPauseCommand = new RelayCommand(async _ => await ExecutePlayPauseAsync());

        // Subscribe to notification events
        _timeTrackingService.TimeLogged += OnTimeLogged;
        _jiraService.AuthenticationFailed += OnAuthenticationFailed;
        _jiraService.AuthenticationCompleted += OnAuthenticationCompleted;

        // Initialize time update timer
        _timeUpdateTimer = new DispatcherTimer
        {
            Interval = Constants.Timeouts.TimeUpdateInterval
        };
        _timeUpdateTimer.Tick += (s, e) =>
        {
            UpdateTimes();
            UpdateTimerDisplay();
        };
        _timeUpdateTimer.Start();

        // Initialize Upwork read timer (single-shot, triggered by window events)
        _upworkReadTimer = new DispatcherTimer
        {
            Interval = Constants.Timeouts.UpworkReadDelay
        };
        _upworkReadTimer.Tick += UpworkReadTimer_Tick;

        // Load settings
        LoadSettings();

        // Initial updates
        UpdateColors();
        UpdateTimes();
        UpdateTimerDisplay();

        // Initialize time tracking mode based on Upwork availability
        InitializeTrackingMode();

        // Start cache service if authenticated
        if (_jiraService.IsAuthenticated)
        {
            _ = _jiraCacheService.StartAsync();
        }

        // Start Upwork window watcher if enabled (for both modes - in Upwork mode it updates time)
        if (Constants.Upwork.EnableWindowWatcher)
        {
            _upworkWindowWatcher.WindowDetected += UpworkWindowWatcher_WindowDetected;
            _upworkWindowWatcher.Start();
        }
    }

    private void InitializeTrackingMode()
    {
        // Check if Upwork is available
        if (_upworkIntegration.IsUpworkAvailable())
        {
            // Read initial weekly total from Upwork
            var weeklyTotal = _upworkIntegration.ReadWeeklyTotal();
            if (weeklyTotal.HasValue)
            {
                _timeTrackingService.InitializeUpworkMode(weeklyTotal.Value);
                System.Diagnostics.Debug.WriteLine($"Initialized in Upwork mode with weekly total: {weeklyTotal.Value}");
            }
            else
            {
                // Upwork window found but couldn't read time - fall back to internal mode
                _timeTrackingService.InitializeInternalMode();
                System.Diagnostics.Debug.WriteLine("Upwork found but couldn't read time - using internal mode");
            }

            // Check if currently tracking
            CheckUpworkState();
        }
        else
        {
            // No Upwork - use internal timer mode
            _timeTrackingService.InitializeInternalMode();
            System.Diagnostics.Debug.WriteLine("Upwork not found - using internal timer mode");
        }
    }

	private void OnTimeLogged(object? sender, WorklogResult result)
    {
        if (result.Success)
        {
            var timeFormatted = FormatTimeLogged(result.TimeLogged);
            NotificationService.ShowSuccess(
                $"{result.IssueKey}. Logged {timeFormatted}",
                null,
                3000);
        }
        else
        {
            NotificationService.ShowError(
                $"Failed to log time to {result.IssueKey}",
                result.ErrorMessage,
                5000);
        }
    }

    private static string FormatTimeLogged(TimeSpan time)
    {
        var hours = (int)time.TotalHours;
        var minutes = time.Minutes;

        if (hours > 0 && minutes > 0)
            return $"{hours}h {minutes}m";
        if (hours > 0)
            return $"{hours}h";
        return $"{minutes}m";
    }

    private void OnAuthenticationFailed(object? sender, string error)
    {
        NotificationService.ShowError(
            "Jira Authentication Failed",
            error,
            5000);
    }

    private void OnAuthenticationCompleted(object? sender, string message)
    {
        NotificationService.ShowSuccess(
            "Jira Connected",
            message,
            3000);

        // Start the cache service now that we're authenticated
        _ = _jiraCacheService.StartAsync();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Load();

        if (!string.IsNullOrEmpty(settings.CustomBackgroundColor))
        {
            try
            {
                CustomBackgroundColor = (WpfColor)WpfColorConverter.ConvertFromString(settings.CustomBackgroundColor);
            }
            catch { }
        }

        Timezones.Clear();
        foreach (var tz in settings.Timezones)
        {
            Timezones.Add(tz);
        }

        // Load last memo
        var lastMemo = Properties.Settings.Default.LastMemo;
        if (!string.IsNullOrEmpty(lastMemo))
        {
            _displayText = lastMemo; // Set directly to avoid triggering save
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    private static void SaveLastMemo(string memo)
    {
        try
        {
            Properties.Settings.Default.LastMemo = memo ?? "";
            Properties.Settings.Default.Save();
        }
        catch { }
    }

    public WindowSettings GetCurrentSettings()
    {
        return new WindowSettings
        {
            CustomBackgroundColor = CustomBackgroundColor?.ToString(),
            Timezones = Timezones.ToList()
        };
    }

    private void UpdateColors()
    {
        var bgColor = CustomBackgroundColor ?? ThemeHelper.GetTaskbarColor();
        var fgColor = ThemeHelper.GetForegroundColor();

        BackgroundBrush = new WpfSolidColorBrush(bgColor);
        ForegroundBrush = new WpfSolidColorBrush(fgColor);

        UpdateTimes();
    }

    private void UpdateTimerDisplay()
    {
        var time = _timeTrackingService.AccumulatedTime;
        var hours = (int)time.TotalHours;
        var minutes = time.Minutes;

        TimerTime = $"{hours}:{minutes:D2}";
    }

    public void UpdateTimes()
    {
        TimeDisplays.Clear();

        foreach (var tz in Timezones)
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tz.TimeZoneId);
                var time = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZone);

                var text = string.IsNullOrWhiteSpace(tz.Caption)
                    ? time.ToString("HH:mm")
                    : $"{tz.Caption}: {time:HH:mm}";

                TimeDisplays.Add(new TimeDisplay { Text = text });
            }
            catch
            {
                // Skip invalid timezones
            }
        }
    }

    private void UpdatePlayPauseIcon()
    {
        // Icons show current STATE (not action to take):
        // - Processing: show grey Play icon (triangle) - inactive
        // - Playing: show green filled Play icon (triangle)
        // - Stopped: show stroke-only Stop icon (square outline)
        if (IsProcessing)
        {
            // Show grey play icon while processing
            PlayPauseIconData = Constants.Icons.PlayIcon;
            IconFillBrush = new WpfSolidColorBrush(WpfColors.Gray);
            IconStrokeBrush = new WpfSolidColorBrush(WpfColors.Transparent);
            IconStrokeThickness = 0;
        }
        else if (IsPlaying)
        {
            PlayPauseIconData = Constants.Icons.PlayIcon;
            IconFillBrush = new WpfSolidColorBrush(WpfColors.LimeGreen);
            IconStrokeBrush = new WpfSolidColorBrush(WpfColors.Transparent);
            IconStrokeThickness = 0;
        }
        else
        {
            PlayPauseIconData = Constants.Icons.StopIcon;
            IconFillBrush = new WpfSolidColorBrush(WpfColors.Transparent);
            IconStrokeBrush = ForegroundBrush;
            IconStrokeThickness = 2;
        }
    }

    private async Task ExecutePlayPauseAsync()
    {
        if (IsProcessing) return;

        var newState = !IsPlaying;

        if (newState)
        {
            // Start tracking - set processing state immediately
            IsPlaying = true;
            IsProcessing = true;

            // Run Upwork automation in background
            _ = Task.Run(() =>
            {
                try
                {
                    _upworkIntegration.UpdateMemo(DisplayText);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Upwork automation failed: {ex.Message}");
                }
            }).ContinueWith(_ =>
            {
                // Always mark as complete (even on failure, treat as success per requirements)
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    IsProcessing = false;
                });
            });

            // Start time tracking with current issue (runs on UI thread, fast)
            _timeTrackingService.Start(DisplayText);

            System.Diagnostics.Debug.WriteLine($"Started tracking for {DisplayText}");
        }
        else
        {
            // Stop tracking - set processing state immediately
            IsPlaying = false;
            IsProcessing = true;

            // Run Upwork stop in background
            _ = Task.Run(() =>
            {
                try
                {
                    _upworkIntegration.ClickStopTracking();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Upwork stop failed: {ex.Message}");
                }
            }).ContinueWith(async _ =>
            {
                // Log remaining time to Jira (can run in background too)
                try
                {
                    await _timeTrackingService.StopAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Time logging failed: {ex.Message}");
                }

                // Always mark as complete
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    IsProcessing = false;
                });
            });

            System.Diagnostics.Debug.WriteLine("Stopped tracking");
        }
    }

    private void CheckUpworkState()
    {
        try
        {
            IsPlaying = _upworkIntegration.IsTracking();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to check Upwork state: {ex.Message}");
        }
    }

    public bool ShowAutocomplete()
    {
        if (IsAutocompleteActive) return false;

        IsAutocompleteActive = true;
        AutocompleteActivated?.Invoke(this, EventArgs.Empty);
		return true;
    }

    public void HideAutocomplete()
    {
        IsAutocompleteActive = false;
    }

    public async void SelectJiraIssue(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        var previousKey = DisplayText;
        DisplayText = key;
        HideAutocomplete();

        // Start time tracking immediately (UI responsive)
        var wasTracking = _timeTrackingService.IsTracking;
        if (wasTracking)
        {
            // Already tracking - handle issue change (log time for previous if >= 10min, keep remainder)
            if (!string.IsNullOrWhiteSpace(previousKey) && previousKey != key)
            {
                await _timeTrackingService.ChangeIssueAsync(key);
            }
        }
        else
        {
            // Not tracking - start tracking
            _timeTrackingService.Start(key);
            IsPlaying = true;
        }

        // Run Upwork automation in background
        IsProcessing = true;
        _ = Task.Run(() =>
        {
            try
            {
                _jiraIssuesService.SelectIssue(key);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Upwork memo update failed: {ex.Message}");
            }
        }).ContinueWith(_ =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                IsProcessing = false;
            });
        });
    }

    public async void SelectJiraIssue(JiraIssue issue)
    {
        if (issue == null || string.IsNullOrWhiteSpace(issue.Key)) return;

        var previousKey = DisplayText;
        DisplayText = issue.Key;
        HideAutocomplete();

        // Start time tracking immediately (UI responsive)
        var wasTracking = _timeTrackingService.IsTracking;
        if (wasTracking)
        {
            // Already tracking - handle issue change (log time for previous if >= 10min, keep remainder)
            if (!string.IsNullOrWhiteSpace(previousKey) && previousKey != issue.Key)
            {
                await _timeTrackingService.ChangeIssueAsync(issue);
            }
        }
        else
        {
            // Not tracking - start tracking
            _timeTrackingService.Start(issue);
            IsPlaying = true;
        }

        // Run Upwork automation in background
        IsProcessing = true;
        _ = Task.Run(() =>
        {
            try
            {
                _jiraIssuesService.SelectIssue(issue.Key);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Upwork memo update failed: {ex.Message}");
            }
        }).ContinueWith(_ =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                IsProcessing = false;
            });
        });
    }

    private void UpworkWindowWatcher_WindowDetected(object? sender, Model.EventArgs.WindowEventArgs e)
    {
        // Only react to window open events in Upwork mode
        if (!e.IsOpen || !_timeTrackingService.IsUpworkMode)
            return;

        // Start timer on FIRST occurrence only (don't restart if already running)
        if (!_upworkReadTimerPending)
        {
            _upworkReadTimerPending = true;
            _upworkReadTimer.Start();
            System.Diagnostics.Debug.WriteLine($"Window detected: {e.Window.Name} - timer started for Upwork read in {Constants.Timeouts.UpworkReadDelay.TotalSeconds}s");
        }
    }

    private void UpworkReadTimer_Tick(object? sender, System.EventArgs e)
    {
        // Stop the timer and reset pending flag
        _upworkReadTimer.Stop();
        _upworkReadTimerPending = false;

        // Read weekly total in background
        _ = Task.Run(() =>
        {
            try
            {
                ReadAndUpdateWeeklyTotal();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Weekly total read failed: {ex.Message}");
            }
        });
    }

    private void ReadAndUpdateWeeklyTotal()
    {
        var weeklyTotal = _upworkIntegration.ReadWeeklyTotal();
        if (weeklyTotal.HasValue)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _timeTrackingService.UpdateWeeklyTotal(weeklyTotal.Value);
                UpdateTimerDisplay();
            });
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Could not read weekly total from Upwork");
        }
    }

    public void StopTimers()
    {
        _timeUpdateTimer.Stop();
        _upworkReadTimer.Stop();
    }

    public void Dispose()
    {
        StopTimers();
        _upworkWindowWatcher?.Dispose();
        _jiraCacheService?.Dispose();
        _upworkIntegration?.Dispose();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
