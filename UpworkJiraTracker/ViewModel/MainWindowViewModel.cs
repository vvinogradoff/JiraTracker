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

    private readonly Dispatcher _dispatcher;
    private readonly UpworkIntegrationFlaUI _upworkIntegration;
    private readonly UpworkWindowWatcherService _upworkWindowWatcher;
    private readonly JiraOAuthService _jiraService;
    private readonly JiraIssueCacheService _jiraCacheService;
    private readonly JiraIssuesService _jiraIssuesService;
    private readonly TimeTrackingService _timeTrackingService;
    private readonly WindowSettingsService _settingsService;
    private readonly DispatcherTimer _timeUpdateTimer;
    private readonly DispatcherTimer _upworkReadTimer;
    private readonly DispatcherTimer _inactivityCheckTimer;
    private bool _upworkReadTimerPending = false;
    private bool _isFreshWeeklyTotal = false;
    private DateTime? _freshTotalResetTime = null;
    private DateTime? _lastTenMinuteAddTime = null;
    private UpworkState _upworkState = UpworkState.NoProcess;
    private TimeStats? _baseTimeStats = null; // Base stats from Excel (without current session)
    private int _pauseOnInactivityMinutes = 0;

    private string _displayText = "Upwork Memo";
    private bool _isAutocompleteActive = false;
    private bool _isPlaying = false;
    private bool _isProcessing = false;
    private bool _isInitializing = true;
    private bool _isUpworkReady = false;
    private bool _isJiraReady = false;
    private WpfColor? _customBackgroundColor = null;
    private WpfBrush _backgroundBrush = new WpfSolidColorBrush(WpfColors.Black);
    private WpfBrush _foregroundBrush = new WpfSolidColorBrush(WpfColors.White);
    private WpfBrush _iconFillBrush = new WpfSolidColorBrush(WpfColors.Transparent);
    private WpfBrush _iconStrokeBrush = new WpfSolidColorBrush(WpfColors.White);
    private double _iconStrokeThickness = 2;
    private string _timerTime = "0:00";
    private string _playPauseIconData = Constants.Icons.StopIcon;
    private string _playPauseTooltip = "No Upwork data";
    private string? _jiraIssueTooltip = null;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? AutocompleteActivated;

    /// <summary>
    /// Delegate to request worklog input from the View.
    /// Returns (comment, remainingEstimateHours, discarded) or (null, null, false) if cancelled.
    /// </summary>
    public Func<(string? Comment, double? RemainingEstimateHours, bool Discarded)>? RequestWorklogInput { get; set; }

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

    public bool IsPaused => _timeTrackingService.IsPaused;

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

    public bool IsInitializing
    {
        get => _isInitializing;
        private set
        {
            if (_isInitializing != value)
            {
                _isInitializing = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsUpworkReady
    {
        get => _isUpworkReady;
        private set
        {
            if (_isUpworkReady != value)
            {
                _isUpworkReady = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsJiraReady
    {
        get => _isJiraReady;
        private set
        {
            if (_isJiraReady != value)
            {
                _isJiraReady = value;
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

    public string PlayPauseTooltip
    {
        get => _playPauseTooltip;
        private set
        {
            if (_playPauseTooltip != value)
            {
                _playPauseTooltip = value;
                OnPropertyChanged();
            }
        }
    }

    public string? JiraIssueTooltip
    {
        get => _jiraIssueTooltip;
        private set
        {
            if (_jiraIssueTooltip != value)
            {
                _jiraIssueTooltip = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsJiraAuthenticated => _jiraService.IsAuthenticated;

    public UpworkState UpworkState
    {
        get => _upworkState;
        private set
        {
            if (_upworkState != value)
            {
                _upworkState = value;
                OnPropertyChanged();
            }
        }
    }

    public UpworkIntegrationFlaUI UpworkIntegration => _upworkIntegration;
    public JiraOAuthService JiraService => _jiraService;
    public JiraIssueCacheService JiraCacheService => _jiraCacheService;
    public JiraIssuesService JiraIssuesService => _jiraIssuesService;
    public TimeTrackingService TimeTrackingService => _timeTrackingService;
    public WindowSettingsService SettingsService => _settingsService;

    public MainWindowViewModel()
    {
        // Capture the dispatcher from the UI thread
        _dispatcher = Dispatcher.CurrentDispatcher;

        // Create services (ViewModel owns the services)
        _upworkIntegration = new UpworkIntegrationFlaUI();
        _jiraService = new JiraOAuthService();
        _settingsService = new WindowSettingsService();
        _upworkWindowWatcher = new UpworkWindowWatcherService();
        _jiraCacheService = new JiraIssueCacheService(_jiraService);
        _jiraIssuesService = new JiraIssuesService(_jiraService, _upworkIntegration, _jiraCacheService);
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
            UpdateTooltipWithCurrentSession();
        };
        _timeUpdateTimer.Start();

        // Initialize Upwork read timer (single-shot, triggered by window events)
        _upworkReadTimer = new DispatcherTimer
        {
            Interval = Constants.Timeouts.UpworkReadDelay
        };
        _upworkReadTimer.Tick += UpworkReadTimer_Tick;

        // Initialize inactivity check timer (checks every 30 seconds)
        _inactivityCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _inactivityCheckTimer.Tick += InactivityCheckTimer_Tick;
        _inactivityCheckTimer.Start();

        // Load settings
        LoadSettings();

        // Initial updates
        UpdateColors();
        UpdateTimes();
        UpdateTimerDisplay();

        // Start Upwork window watcher if enabled (for both modes - in Upwork mode it updates time)
        if (Constants.Upwork.EnableWindowWatcher)
        {
            _upworkWindowWatcher.WindowDetected += UpworkWindowWatcher_WindowDetected;
            _upworkWindowWatcher.Start();
        }
    }

    /// <summary>
    /// Async initialization that must be called after construction.
    /// Call this from Window.Loaded event.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Start Upwork discovery in background
        _ = Task.Run(async () =>
        {
            await InitializeTrackingModeAsync();

            // Update UI on main thread
            _dispatcher.Invoke(() =>
            {
                IsUpworkReady = true;
                // Check if fully initialized
                if (IsJiraReady)
                {
                    IsInitializing = false;
                }
            });
        });

        // Jira check is instant (just checking if authenticated)
        IsJiraReady = true;
        if (_jiraService.IsAuthenticated)
        {
            _ = _jiraCacheService.StartAsync();
        }

        // Update initialization state
        if (IsUpworkReady && IsJiraReady)
        {
            IsInitializing = false;
        }
    }

    private async Task InitializeTrackingModeAsync()
    {
        // Check if Upwork is available
        if (_upworkIntegration.IsUpworkAvailable())
        {
            // Read initial time stats from Upwork (single enumeration for both stats and weekly total)
            var (timeStats, weeklyTotal) = await _upworkIntegration.ReadAllTimeData();

            // Update tooltip with initial stats
            if (timeStats != null)
            {
                // Create tooltip string on background thread (strings are thread-safe)
                var tooltipText = timeStats.ToTooltipString();

                _dispatcher.Invoke(() =>
                {
                    PlayPauseTooltip = tooltipText;
                });
            }

            if (weeklyTotal.HasValue)
            {
                _timeTrackingService.InitializeUpworkMode(weeklyTotal.Value);
                _dispatcher.Invoke(() => UpworkState = UpworkState.FullyAutomated);
                System.Diagnostics.Debug.WriteLine($"Initialized in Upwork mode with weekly total: {weeklyTotal.Value}");
            }
            else
            {
                // Upwork window found but couldn't read time - fall back to internal mode
                _timeTrackingService.InitializeInternalMode();
                _dispatcher.Invoke(() => UpworkState = UpworkState.ProcessFoundButCannotAutomate);
                System.Diagnostics.Debug.WriteLine("Upwork found but couldn't read time - using internal mode");

                // Load time stats from Excel file since we can't read from Upwork (async to avoid UI freeze)
                System.Diagnostics.Debug.WriteLine("[MainWindowViewModel] Loading time stats from Excel (ProcessFoundButCannotAutomate)");
                var timeLogService = _timeTrackingService.TimeLogService;
                var stats = await timeLogService.CalculateStatsAsync();
                var tooltipText = stats.ToTooltipString();
                System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Stats loaded: {tooltipText}");

                _dispatcher.Invoke(() =>
                {
                    _baseTimeStats = stats;
                    PlayPauseTooltip = tooltipText;
                    System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Tooltip set to: {PlayPauseTooltip}");
                });
            }

            // Check if currently tracking
            CheckUpworkState();
        }
        else
        {
            // No Upwork - use internal timer mode
            _timeTrackingService.InitializeInternalMode();
            _dispatcher.Invoke(() => UpworkState = UpworkState.NoProcess);
            System.Diagnostics.Debug.WriteLine("Upwork not found - using internal timer mode");

            // Load time stats from Excel file (async to avoid UI freeze)
            System.Diagnostics.Debug.WriteLine("[MainWindowViewModel] Loading time stats from Excel (NoProcess)");
            var timeLogService = _timeTrackingService.TimeLogService;
            var stats = await timeLogService.CalculateStatsAsync();
            var tooltipText = stats.ToTooltipString();
            System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Stats loaded: {tooltipText}");

            _dispatcher.Invoke(() =>
            {
                _baseTimeStats = stats;
                PlayPauseTooltip = tooltipText;
                System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Tooltip set to: {PlayPauseTooltip}");
            });
        }
    }

	private async void OnTimeLogged(object? sender, WorklogResult result)
    {
        if (result.Success)
        {
            var timeFormatted = FormatTimeLogged(result.TimeLogged);
            NotificationService.ShowSuccess(
                $"{result.IssueKey}. Logged {timeFormatted}",
                null,
                3000);

            // Refresh tooltip stats after successful time logging (only in non-Upwork mode)
            if (!_timeTrackingService.IsUpworkMode)
            {
                await RefreshTimeStatsTooltipAsync();
            }
        }
        else
        {
            NotificationService.ShowError(
                $"Failed to log time to {result.IssueKey}",
                result.ErrorMessage,
                5000);
        }
    }

    /// <summary>
    /// Refreshes the play/pause tooltip with current time stats from the Excel file.
    /// Also updates the base stats cache.
    /// </summary>
    private async Task RefreshTimeStatsTooltipAsync()
    {
        try
        {
            var timeLogService = _timeTrackingService.TimeLogService;
            // Invalidate cache to force re-read from disk after logging
            timeLogService.InvalidateCache();
            var stats = await timeLogService.CalculateStatsAsync();
            var tooltipText = stats.ToTooltipString();

            _dispatcher.Invoke(() =>
            {
                _baseTimeStats = stats;
                PlayPauseTooltip = tooltipText;
                System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Tooltip refreshed: {PlayPauseTooltip}");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Failed to refresh tooltip stats: {ex.Message}");
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
                // Handle special "Transparent" value explicitly
                if (settings.CustomBackgroundColor.Equals("Transparent", StringComparison.OrdinalIgnoreCase))
                {
                    CustomBackgroundColor = WpfColor.FromArgb(0, 0, 0, 0);
                }
                else
                {
                    CustomBackgroundColor = (WpfColor)WpfColorConverter.ConvertFromString(settings.CustomBackgroundColor);
                }
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
            UpdateJiraIssueTooltip(lastMemo);
        }

        // Load pause on inactivity setting
        _pauseOnInactivityMinutes = settings.PauseOnInactivityMinutes;
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

        // Use nearly-transparent (alpha=1) instead of fully transparent (alpha=0)
        // to enable hit-testing while still appearing transparent
        if (bgColor.A == 0)
        {
            bgColor = WpfColor.FromArgb(1, bgColor.R, bgColor.G, bgColor.B);
        }

        BackgroundBrush = new WpfSolidColorBrush(bgColor);
        ForegroundBrush = new WpfSolidColorBrush(fgColor);

        UpdateTimes();
    }

    private void UpdateTimerDisplay()
    {
        var time = _timeTrackingService.AccumulatedTime;

        TimerTime = $"{(int)time.TotalHours}:{time.Minutes:D2}";

        // Check if we need to reset the fresh indicator
        if (_isFreshWeeklyTotal && _freshTotalResetTime.HasValue && DateTime.Now >= _freshTotalResetTime.Value)
        {
            _isFreshWeeklyTotal = false;
            _freshTotalResetTime = null;
            System.Diagnostics.Debug.WriteLine("Fresh weekly total indicator reset");
            UpdatePlayPauseIcon();
        }
    }

    /// <summary>
    /// Updates the tooltip to show base stats plus current session time (non-Upwork mode only).
    /// </summary>
    private void UpdateTooltipWithCurrentSession()
    {
        // Only update in non-Upwork mode when we have base stats
        if (_timeTrackingService.IsUpworkMode || _baseTimeStats == null)
            return;

        // Calculate current session time
        var currentSessionHours = _timeTrackingService.AccumulatedTime.TotalHours;

        // Create combined stats with current session added to today, this week, and this month
        var combinedStats = new TimeStats
        {
            TodayHours = _baseTimeStats.TodayHours + currentSessionHours,
            ThisWeekHours = _baseTimeStats.ThisWeekHours + currentSessionHours,
            ThisMonthHours = _baseTimeStats.ThisMonthHours + currentSessionHours
        };

        PlayPauseTooltip = combinedStats.ToTooltipString();
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
        // - Paused: show stroke-only Pause icon (two vertical bars) - same colors as stop
        // - Playing: show green filled Play icon (triangle)
        //   - With fresh weekly total in Upwork mode: add lightgreen outline
        // - Stopped: show stroke-only Stop icon (square outline)
        if (IsProcessing)
        {
            // Show grey play icon while processing
            PlayPauseIconData = Constants.Icons.PlayIcon;
            IconFillBrush = new WpfSolidColorBrush(WpfColors.Gray);
            IconStrokeBrush = new WpfSolidColorBrush(WpfColors.Transparent);
            IconStrokeThickness = 0;
        }
        else if (IsPaused)
        {
            // Show pause icon with stroke (same styling as stop)
            PlayPauseIconData = Constants.Icons.PauseIcon;
            IconFillBrush = new WpfSolidColorBrush(WpfColors.Transparent);
            IconStrokeBrush = ForegroundBrush;
            IconStrokeThickness = 2;
        }
        else if (IsPlaying)
        {
            PlayPauseIconData = Constants.Icons.PlayIcon;
            IconFillBrush = new WpfSolidColorBrush(WpfColors.LimeGreen);

            // Show lightgreen outline if we have fresh weekly total in Upwork mode
            if (_isFreshWeeklyTotal && _timeTrackingService.IsUpworkMode)
            {
                IconStrokeBrush = new WpfSolidColorBrush(WpfColors.LightGreen);
                IconStrokeThickness = 2;
            }
            else
            {
                IconStrokeBrush = new WpfSolidColorBrush(WpfColors.Transparent);
                IconStrokeThickness = 0;
            }
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

        // Handle resume from paused state
        if (IsPaused)
        {
            _timeTrackingService.Resume();
            OnPropertyChanged(nameof(IsPaused));
            UpdatePlayPauseIcon();
            System.Diagnostics.Debug.WriteLine("Resumed tracking from paused state");
            return;
        }

        var newState = !IsPlaying;

        if (newState)
        {
            // Start tracking - set processing state immediately
            IsPlaying = true;
            IsProcessing = true;

            // Run Upwork automation in background
            _ = Task.Run(async () =>
            {
                try
                {
                    _upworkIntegration.UpdateMemo(DisplayText);

                    // If in Upwork mode, read weekly total after starting to get accurate time
                    if (_timeTrackingService.IsUpworkMode)
                    {
                        await ReadAndUpdateWeeklyTotalAsync();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Upwork automation failed: {ex.Message}");
                }
            }).ContinueWith(_ =>
            {
                // Always mark as complete (even on failure, treat as success per requirements)
                _dispatcher.Invoke(() =>
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

            // Request worklog input from UI (runs on UI thread before background work)
            string? worklogComment = null;
            double? remainingEstimate = null;
            bool shouldDiscard = false;

            if (RequestWorklogInput != null)
            {
                var (comment, estimate, discarded) = RequestWorklogInput();
                worklogComment = comment;
                remainingEstimate = estimate;
                shouldDiscard = discarded;
            }

            // Handle discard case - cancel tracking without logging to Jira/Deel
            if (shouldDiscard)
            {
                _timeTrackingService.Cancel();
                IsProcessing = false;
                UpdateTimerDisplay();
                System.Diagnostics.Debug.WriteLine("Discarded tracking - time not logged to Jira/Deel");
                return;
            }

            // Run Upwork stop in background
            _ = Task.Run(async () =>
            {
                try
                {
                    _upworkIntegration.ClickStopTracking();

                    // Read actual time stats and weekly totals after clicking stop (window is now visible)
                    if (_timeTrackingService.IsUpworkMode)
                    {
                        // Read all time data in single enumeration
                        var (timeStats, actualWeeklyTotal) = await _upworkIntegration.ReadAllTimeData();

                        if (timeStats != null)
                        {
                            // Create tooltip string on background thread (strings are thread-safe)
                            var tooltipText = timeStats.ToTooltipString();

                            await _dispatcher.InvokeAsync(() =>
                            {
                                PlayPauseTooltip = tooltipText;
                            });
                        }

                        if (actualWeeklyTotal.HasValue)
                        {
                            System.Diagnostics.Debug.WriteLine($"Read actual weekly total after stop: {actualWeeklyTotal.Value}");

                            // Update with real value for accurate Jira logging
                            await _dispatcher.InvokeAsync(() =>
                            {
                                _timeTrackingService.UpdateWeeklyTotal(actualWeeklyTotal.Value);
                                UpdateTimerDisplay();
                            });
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Could not read actual weekly total after stop, using calculated value");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Upwork stop failed: {ex.Message}");
                }

                // Log remaining time to Jira with optional comment and estimate
                try
                {
                    await _timeTrackingService.StopAsync(worklogComment, remainingEstimate);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Time logging failed: {ex.Message}");
                }

                // Always mark as complete
                _dispatcher.Invoke(() =>
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
            var isTracking = _upworkIntegration.CheckIsTracking();
            _dispatcher.Invoke(() => IsPlaying = isTracking);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to check Upwork state: {ex.Message}");
        }
    }

    public bool ShowAutocomplete()
    {
        if (IsAutocompleteActive) return false;
        if (!IsJiraReady) return false; // Don't show autocomplete until Jira is ready

        IsAutocompleteActive = true;
        AutocompleteActivated?.Invoke(this, EventArgs.Empty);
		return true;
    }

    public void HideAutocomplete()
    {
        IsAutocompleteActive = false;
    }

    public Task SelectJiraIssue(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return Task.CompletedTask;

		var issue = _timeTrackingService.GetCachedIssue(key);
		if (issue == null)
		{
			System.Diagnostics.Debug.WriteLine($"Warning: Issue {key} not found in cache, using key only");
			issue = new IssueDetails(new JiraIssue { Key = key });
		}

		return SelectJiraIssue(issue);
	}

    public Task SelectJiraIssue(IssueDetails issue)
    {
        if (issue == null || string.IsNullOrWhiteSpace(issue.Key)) return Task.CompletedTask;

        var previousKey = DisplayText;
        DisplayText = issue.Key;
        UpdateJiraIssueTooltip(issue);
        HideAutocomplete();

        var wasTracking = _timeTrackingService.IsTracking;
        var shouldLogPrevious = wasTracking && !string.IsNullOrWhiteSpace(previousKey) && previousKey != issue.Key;

        // Request worklog input if we're about to log time for previous issue
        string? worklogComment = null;
        double? remainingEstimate = null;
        bool shouldDiscard = false;

        if (shouldLogPrevious && RequestWorklogInput != null)
        {
            var (comment, estimate, discarded) = RequestWorklogInput();
            worklogComment = comment;
            remainingEstimate = estimate;
            shouldDiscard = discarded;
        }

        // Handle discard case - cancel tracking without logging to Jira/Deel
        if (shouldDiscard)
        {
            _timeTrackingService.Cancel();
            // Start tracking new issue immediately
            _timeTrackingService.Start(issue);
            UpdateTimerDisplay();
            System.Diagnostics.Debug.WriteLine($"Discarded previous tracking, started new issue {issue.Key}");
            return Task.CompletedTask;
        }

        // Run Upwork automation first
        IsProcessing = true;
        return Task.Run(async () =>
        {
            try
            {
                _jiraIssuesService.SelectIssue(issue.Key);

                // Read actual time stats and weekly totals after updating Upwork (window is now visible)
                if (shouldLogPrevious && _timeTrackingService.IsUpworkMode)
                {
                    // Read all time data in single enumeration
                    var (timeStats, actualWeeklyTotal) = await _upworkIntegration.ReadAllTimeData();

                    if (timeStats != null)
                    {
                        // Create tooltip string on background thread (strings are thread-safe)
                        var tooltipText = timeStats.ToTooltipString();

                        await _dispatcher.InvokeAsync(() =>
                        {
                            PlayPauseTooltip = tooltipText;
                        });
                    }

                    if (actualWeeklyTotal.HasValue)
                    {
                        System.Diagnostics.Debug.WriteLine($"Read actual weekly total after issue change: {actualWeeklyTotal.Value}");

                        // Update with real value for accurate Jira logging
                        await _dispatcher.InvokeAsync(() =>
                        {
                            _timeTrackingService.UpdateWeeklyTotal(actualWeeklyTotal.Value);
                            UpdateTimerDisplay();
                        });
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Could not read actual weekly total after issue change, using calculated value");
                    }
                }

                // Now log time with actual values
                if (shouldLogPrevious)
                {
                    // Was tracking and switching to different issue - log previous time and switch
                    await _timeTrackingService.ChangeIssueAsync(issue, worklogComment, remainingEstimate);
                    System.Diagnostics.Debug.WriteLine($"Changed issue to {issue.Key}, time logged for previous");
                }
                else if (!wasTracking)
                {
                    // Not tracking - start tracking
                    _dispatcher.Invoke(() =>
                    {
                        _timeTrackingService.Start(issue);
                        IsPlaying = true;
                    });
                    System.Diagnostics.Debug.WriteLine($"Started tracking {issue.Key}");
                }
                else
                {
                    // Was tracking same issue - just update memo, no logging needed
                    System.Diagnostics.Debug.WriteLine($"Same issue {issue.Key}, memo updated only");
                }

                // Ensure timer display is updated
                _dispatcher.Invoke(() =>
                {
                    UpdateTimerDisplay();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Upwork memo update failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception details: {ex}");
            }

            _dispatcher.Invoke(() =>
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

        // Marshal to UI thread since DispatcherTimer must be started from UI thread
        _dispatcher.BeginInvoke(() =>
        {
            // Start timer on FIRST occurrence only (don't restart if already running)
            if (!_upworkReadTimerPending)
            {
                _upworkReadTimerPending = true;
                _upworkReadTimer.Start();
                System.Diagnostics.Debug.WriteLine($"Window detected: {e.Window.Name} - timer started for Upwork read in {Constants.Timeouts.UpworkReadDelay.TotalSeconds}s");
            }
        });
    }

    private void UpworkReadTimer_Tick(object? sender, System.EventArgs e)
    {
        // Stop the timer and reset pending flag
        _upworkReadTimer.Stop();
        _upworkReadTimerPending = false;

        // Read weekly total in background
        _ = Task.Run(async () =>
        {
            try
            {
                await ReadAndUpdateWeeklyTotalAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Weekly total read failed: {ex.Message}");
            }
        });
    }

    private void InactivityCheckTimer_Tick(object? sender, System.EventArgs e)
    {
        // Skip if auto-pause is disabled or not tracking or already paused
        if (_pauseOnInactivityMinutes <= 0 || !IsPlaying || IsPaused)
            return;

        // Get idle time in milliseconds
        var idleTimeMs = NativeMethods.GetIdleTime();
        var idleMinutes = idleTimeMs / 60000.0;

        System.Diagnostics.Debug.WriteLine($"Inactivity check: {idleMinutes:F1} minutes idle (threshold: {_pauseOnInactivityMinutes} min)");

        // Check if idle time exceeds threshold
        if (idleMinutes >= _pauseOnInactivityMinutes)
        {
            // Auto-pause tracking
            _timeTrackingService.Pause();
            OnPropertyChanged(nameof(IsPaused));
            UpdatePlayPauseIcon();

            // Show toast notification
            NotificationService.ShowSuccess(
                "Time tracking paused",
                $"Paused after {_pauseOnInactivityMinutes} minutes of inactivity",
                5000);

            System.Diagnostics.Debug.WriteLine($"Auto-paused tracking after {idleMinutes:F1} minutes of inactivity");
        }
    }

    /// <summary>
    /// Reloads the pause on inactivity setting from user settings.
    /// Called when settings are changed.
    /// </summary>
    public void ReloadPauseOnInactivitySetting()
    {
        _pauseOnInactivityMinutes = Properties.Settings.Default.PauseOnInactivityMinutes;
        System.Diagnostics.Debug.WriteLine($"Pause on inactivity setting reloaded: {_pauseOnInactivityMinutes} minutes");
    }

    private async Task ReadAndUpdateWeeklyTotalAsync()
    {
        // Read all time data in single enumeration
        var (timeStats, weeklyTotal) = await _upworkIntegration.ReadAllTimeData();

        // Update tooltip with latest stats
        if (timeStats != null)
        {
            // Create tooltip string on background thread (strings are thread-safe)
            var tooltipText = timeStats.ToTooltipString();

            _dispatcher.Invoke(() =>
            {
                PlayPauseTooltip = tooltipText;
            });
        }
        if (weeklyTotal.HasValue)
        {
            _dispatcher.Invoke(() =>
            {
                var previousTotal = _timeTrackingService.WeeklyTotal;
                var readValue = weeklyTotal.Value;
                var now = DateTime.Now;

                // Calculate current 10-minute segment boundary
                var currentSegmentStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, (now.Minute / 10) * 10, 0);

                // Check if value changed from what Upwork reports
                if (previousTotal != readValue)
                {
                    // Upwork UI updated - use new value
                    _timeTrackingService.UpdateWeeklyTotal(readValue);
                    _lastTenMinuteAddTime = null; // Reset fallback tracking
                    _isFreshWeeklyTotal = true;

                    var currentMinute = now.Minute;
                    var nextBoundaryMinute = ((currentMinute / 10) + 1) * 10;

                    if (nextBoundaryMinute >= 60)
                    {
                        _freshTotalResetTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
                    }
                    else
                    {
                        _freshTotalResetTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, nextBoundaryMinute, 0);
                    }

                    System.Diagnostics.Debug.WriteLine($"Upwork UI updated: {previousTotal} -> {readValue}. Fresh indicator until {_freshTotalResetTime:HH:mm:ss}");
                    UpdatePlayPauseIcon();
                }
                else
                {
                    // Same value - Upwork UI hasn't updated (window likely hidden)
                    // Fallback: add 10 minutes if we haven't already in this segment
                    bool canAddTenMinutes = !_lastTenMinuteAddTime.HasValue ||
                                           _lastTenMinuteAddTime.Value < currentSegmentStart;

                    if (canAddTenMinutes)
                    {
                        var adjustedTotal = previousTotal.Add(TimeSpan.FromMinutes(10));
                        _timeTrackingService.UpdateWeeklyTotal(adjustedTotal);
                        _lastTenMinuteAddTime = now;
                        _isFreshWeeklyTotal = true;

                        var currentMinute = now.Minute;
                        var nextBoundaryMinute = ((currentMinute / 10) + 1) * 10;

                        if (nextBoundaryMinute >= 60)
                        {
                            _freshTotalResetTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
                        }
                        else
                        {
                            _freshTotalResetTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, nextBoundaryMinute, 0);
                        }

                        System.Diagnostics.Debug.WriteLine($"Upwork UI stale ({readValue}). Fallback: added 10min -> {adjustedTotal}. Fresh indicator until {_freshTotalResetTime:HH:mm:ss}");
                        UpdatePlayPauseIcon();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Upwork UI stale ({readValue}), already added 10min in this segment at {_lastTenMinuteAddTime:HH:mm:ss}");
                    }
                }

                UpdateTimerDisplay();
            });
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Could not read weekly total from Upwork");
        }
    }

    private void UpdateJiraIssueTooltip(string issueKey)
    {
        if (string.IsNullOrWhiteSpace(issueKey))
        {
            JiraIssueTooltip = null;
            return;
        }

        var cachedIssue = _timeTrackingService.GetCachedIssue(issueKey);
        if (cachedIssue != null)
        {
            UpdateJiraIssueTooltip(cachedIssue);
        }
        else
        {
            JiraIssueTooltip = issueKey;
        }
    }

    private void UpdateJiraIssueTooltip(IssueDetails issue)
    {
        if (issue == null || string.IsNullOrWhiteSpace(issue.Key))
        {
            JiraIssueTooltip = null;
            return;
        }

        var parts = new List<string> { issue.Key };

        if (!string.IsNullOrWhiteSpace(issue.Assignee))
            parts.Add(issue.Assignee);

        if (!string.IsNullOrWhiteSpace(issue.Status))
            parts.Add(issue.Status);

        var firstLine = string.Join(" | ", parts);

        if (!string.IsNullOrWhiteSpace(issue.Summary))
        {
            JiraIssueTooltip = $"{firstLine}\n{issue.Summary}";
        }
        else
        {
            JiraIssueTooltip = firstLine;
        }
    }

    public void StopTimers()
    {
        _timeUpdateTimer.Stop();
        _upworkReadTimer.Stop();
        _inactivityCheckTimer.Stop();
    }

    public void Dispose()
    {
        StopTimers();
        _upworkWindowWatcher?.Dispose();
        _jiraCacheService?.Dispose();
        _upworkIntegration?.Dispose();

        // Close Deel browser before exiting
        try
        {
            _timeTrackingService?.CleanupAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Failed to cleanup Deel: {ex.Message}");
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
