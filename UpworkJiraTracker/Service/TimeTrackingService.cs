using UpworkJiraTracker.Model;

namespace UpworkJiraTracker.Service;

public class TimeTrackingService
{
    private readonly JiraIssuesService _jiraIssuesService;
    private readonly TimeLogService _timeLogService;

    // Issue cache for looking up issue details by key
    private readonly Dictionary<string, IssueDetails> _issueCache = new();

    private IssueDetails? _currentIssue;
    private TimeSpan _accumulatedTime;
    private DateTime _trackingStartedAtUtc;
    private bool _isTracking;

    // Upwork mode tracking
    private bool _isUpworkMode;
    private TimeSpan _baseWeeklyTotal;
    private TimeSpan _currentWeeklyTotal;

    public bool IsTracking => _isTracking;
    public string? CurrentIssueKey => _currentIssue?.Key;
    public TimeSpan AccumulatedTime => _isUpworkMode
        ? (_currentWeeklyTotal - _baseWeeklyTotal)
        : (_accumulatedTime + GetElapsedSinceStart());

    /// <summary>
    /// True if running in Upwork mode (time comes from Upwork window).
    /// False if running in internal timer mode.
    /// </summary>
    public bool IsUpworkMode => _isUpworkMode;

    /// <summary>
    /// The current weekly total from Upwork (only valid in Upwork mode)
    /// </summary>
    public TimeSpan WeeklyTotal => _currentWeeklyTotal;

    /// <summary>
    /// The base weekly total set at startup (only valid in Upwork mode)
    /// </summary>
    public TimeSpan BaseWeeklyTotal => _baseWeeklyTotal;

    public event EventHandler<WorklogResult>? TimeLogged;

    public TimeTrackingService(JiraIssuesService jiraIssuesService)
    {
        _jiraIssuesService = jiraIssuesService;
        _timeLogService = new TimeLogService();
        _jiraIssuesService.WorklogCompleted += OnWorklogCompleted;
    }

    /// <summary>
    /// Initialize in Upwork mode with the initial weekly total
    /// </summary>
    public void InitializeUpworkMode(TimeSpan initialWeeklyTotal)
    {
        _isUpworkMode = true;
        _baseWeeklyTotal = initialWeeklyTotal;
        _currentWeeklyTotal = initialWeeklyTotal;
        System.Diagnostics.Debug.WriteLine($"Initialized Upwork mode with base total: {initialWeeklyTotal}");
    }

    /// <summary>
    /// Initialize in internal timer mode (no Upwork)
    /// </summary>
    public void InitializeInternalMode()
    {
        _isUpworkMode = false;
        _baseWeeklyTotal = TimeSpan.Zero;
        _currentWeeklyTotal = TimeSpan.Zero;
        System.Diagnostics.Debug.WriteLine("Initialized internal timer mode");
    }

    /// <summary>
    /// Update the weekly total from Upwork (only in Upwork mode).
    /// The difference from base is calculated as time spent.
    /// </summary>
    public void UpdateWeeklyTotal(TimeSpan newWeeklyTotal)
    {
        if (!_isUpworkMode)
        {
            System.Diagnostics.Debug.WriteLine("Warning: UpdateWeeklyTotal called but not in Upwork mode");
            return;
        }

        var previousTotal = _currentWeeklyTotal;
        _currentWeeklyTotal = newWeeklyTotal;
        var timeDiff = newWeeklyTotal - previousTotal;
        var totalAccumulated = newWeeklyTotal - _baseWeeklyTotal;

        System.Diagnostics.Debug.WriteLine($"Weekly total updated: {previousTotal} -> {newWeeklyTotal} (diff: {timeDiff}, total accumulated: {totalAccumulated})");
    }

    private void OnWorklogCompleted(object? sender, WorklogResult result)
    {
        TimeLogged?.Invoke(this, result);

        // Log time entry to file
        if (result.Success && _currentIssue != null)
        {
            _timeLogService.LogTime(
                _currentIssue.Key,
                _currentIssue.Summary,
                _currentIssue.Assignee,
                _currentIssue.Status,
                result.TimeLogged);
        }
    }

    /// <summary>
    /// Cache an issue for later lookup by key.
    /// </summary>
    public void CacheIssue(JiraIssue issue)
    {
        if (!string.IsNullOrWhiteSpace(issue.Key))
        {
            _issueCache[issue.Key] = new IssueDetails(issue);
        }
    }

    /// <summary>
    /// Get cached issue details by key.
    /// </summary>
    public IssueDetails? GetCachedIssue(string key)
		=> _issueCache.TryGetValue(key, out var issue) ? issue : null;

    /// <summary>
    /// Start tracking time with a Jira issue.
    /// In internal mode: calculates initial offset based on current time.
    /// In Upwork mode: no offset, time comes from Upwork.
    /// </summary>
    public void Start(IssueDetails details)
    {
        _issueCache[details.Key] = details;
        StartInternal(details);
    }

    /// <summary>
    /// Start tracking time with cached issue details.
    /// Falls back to key-only tracking if issue not found in cache.
    /// </summary>
    public void Start(string issueKey)
    {
        var details = GetCachedIssue(issueKey);
        if (details == null)
        {
            System.Diagnostics.Debug.WriteLine($"Warning: Issue {issueKey} not found in cache, tracking with key only");
            details = new IssueDetails(new JiraIssue { Key = issueKey });
            _issueCache[issueKey] = details;
        }
        StartInternal(details);
    }

    private void StartInternal(IssueDetails issue)
    {
        if (_isTracking)
        {
            System.Diagnostics.Debug.WriteLine("Already tracking, call Stop first");
            return;
        }

        _currentIssue = issue;
        _trackingStartedAtUtc = DateTime.UtcNow;

        if (_isUpworkMode)
        {
            // In Upwork mode, no initial offset - time comes from Upwork
            _accumulatedTime = TimeSpan.Zero;
        }
        else
        {
            // In internal mode, don't round - count as-is
            _accumulatedTime = TimeSpan.Zero;
        }

        _isTracking = true;

        // Log start event
        _timeLogService.LogStart(issue.Key, issue.Summary, issue.Assignee, issue.Status);

        System.Diagnostics.Debug.WriteLine($"Started tracking {issue.Key} (Upwork mode: {_isUpworkMode})");
    }

    /// <summary>
    /// Change the current issue being tracked with a Jira issue.
    /// In internal mode: logs time as-is without rounding.
    /// In Upwork mode: logs time based on Upwork's tracked time.
    /// </summary>
    public async Task ChangeIssueAsync(IssueDetails details)
    {
        _issueCache[details.Key] = details;
        await ChangeIssueInternalAsync(details);
    }

    private async Task ChangeIssueInternalAsync(IssueDetails newIssue)
    {
        if (!_isTracking)
        {
            System.Diagnostics.Debug.WriteLine("Not tracking, nothing to change");
            return;
        }

        if (_currentIssue == null || string.IsNullOrWhiteSpace(_currentIssue.Key))
        {
            _currentIssue = newIssue;
            _timeLogService.LogSwitch(newIssue.Key, newIssue.Summary, newIssue.Assignee, newIssue.Status);
            return;
        }

        if (_currentIssue.Key == newIssue.Key)
        {
            return;
        }

        // Calculate total time for previous issue
        var totalTime = _isUpworkMode
            ? (_currentWeeklyTotal - _baseWeeklyTotal)
            : (_accumulatedTime + GetElapsedSinceStart());

        System.Diagnostics.Debug.WriteLine($"Issue change: {_currentIssue.Key} -> {newIssue.Key}, Total time: {totalTime}");

        // Log switch event
        _timeLogService.LogSwitch(newIssue.Key, newIssue.Summary, newIssue.Assignee, newIssue.Status);

        if (_isUpworkMode)
        {
            // In Upwork mode, don't log zero time
            if (totalTime > TimeSpan.Zero)
            {
                await _jiraIssuesService.LogTimeAsync(_currentIssue.Key, totalTime);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Upwork mode: zero time, not logging to Jira");
            }

            // Reset base to current for new issue
            _baseWeeklyTotal = _currentWeeklyTotal;
        }
        else
        {
            // In internal mode, don't log if less than 2 minutes
            var minTime = Constants.TimeTracking.MinimumTimeInternalMode;
            if (totalTime >= minTime)
            {
                await _jiraIssuesService.LogTimeAsync(_currentIssue.Key, totalTime);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Internal mode: {totalTime} < {minTime}, not logging to Jira");
            }
        }

        // Reset for new issue
        _currentIssue = newIssue;
        _trackingStartedAtUtc = DateTime.UtcNow;
        _accumulatedTime = TimeSpan.Zero;
    }

    /// <summary>
    /// Stop tracking. Logs time as-is without rounding.
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isTracking)
        {
            System.Diagnostics.Debug.WriteLine("Not tracking, nothing to stop");
            return;
        }

        if (_currentIssue == null || string.IsNullOrWhiteSpace(_currentIssue.Key))
        {
            _isTracking = false;
            _accumulatedTime = TimeSpan.Zero;
            return;
        }

        // Log stop event
        _timeLogService.LogStop(_currentIssue.Key, _currentIssue.Summary, _currentIssue.Assignee, _currentIssue.Status);

        // Calculate total time
        var totalTime = _isUpworkMode
            ? (_currentWeeklyTotal - _baseWeeklyTotal)
            : (_accumulatedTime + GetElapsedSinceStart());

        System.Diagnostics.Debug.WriteLine($"Stop tracking: Total time: {totalTime}");

        // Check minimum time thresholds before logging
        if (_isUpworkMode)
        {
            // In Upwork mode, don't log zero time
            if (totalTime > TimeSpan.Zero)
            {
                await _jiraIssuesService.LogTimeAsync(_currentIssue.Key, totalTime);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Upwork mode: zero time, not logging to Jira");
            }
        }
        else
        {
            // In internal mode, don't log if less than 2 minutes
            var minTime = Constants.TimeTracking.MinimumTimeInternalMode;
            if (totalTime >= minTime)
            {
                await _jiraIssuesService.LogTimeAsync(_currentIssue.Key, totalTime);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Internal mode: {totalTime} < {minTime}, not logging to Jira");
            }
        }

        // Reset state
        _isTracking = false;
        _currentIssue = null;
        _accumulatedTime = TimeSpan.Zero;

        if (_isUpworkMode)
        {
            // Reset base to current for next session
            _baseWeeklyTotal = _currentWeeklyTotal;
        }
    }

    private TimeSpan GetElapsedSinceStart()
    {
        if (!_isTracking)
            return TimeSpan.Zero;

        return DateTime.UtcNow - _trackingStartedAtUtc;
    }
}
