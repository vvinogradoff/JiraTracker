namespace UpworkJiraTracker;

public static class Constants
{
    public static class Upwork
    {
        public static string ProcessName => "Upwork";
        public static string WindowTitle => "Time Tracker";
        public static string MemoFieldLabel => "What are you working on?";
        public static string UpworkStartTooltip => "Upwork desktop tracker application must be start with --enable-features=UiaProvider";

        /// <summary>
        /// When true, forces Upwork window to foreground during automation.
        /// When false, attempts to perform automation without stealing focus.
        /// </summary>
        public static bool ForceFocusWindow => true;

        /// <summary>
        /// When true, monitors for new Upwork windows and logs them.
        /// Use for debugging to identify screenshot confirmation windows.
        /// </summary>
        public static bool EnableWindowWatcher => true;

        public static class AutomationIds
        {
            public static string RestoreButton => "Minimize-Restore";
            public static string MaximizeButton => "Maximize-Restore";
            public static string CloseButton => "Close";
        }

        public static class ButtonText
        {
            public static string Start => "Start";
            public static string Stop => "Stop";
            public static string StartTracking => "Start tracking";
            public static string StopTracking => "Stop tracking";
            public static string Edit => "Edit";
            public static string Update => "Update";
        }
    }

    public static class Window
    {
        public static class MainWindow
        {
            public static string Title => "UpworkJiraTracker";
            public static double DefaultWidth => 180;
            public static double DefaultHeight => 48;
        }

        public static class Settings
        {
            public static string Title => "Settings";
            public static double Width => 300;
            public static double Height => 220;
        }
    }

    public static class Timezones
    {
        public static string DefaultTimezone1 => "Pacific Standard Time"; // GMT-8
        public static string DefaultTimezone2 => "GMT Standard Time"; // GMT
    }

    public static class UI
    {
        public static int DragThresholdPixels => 4;
        public static string NothingFound => "Nothing found";
    }

    public static class Timeouts
    {
        public static TimeSpan ShortDelay => TimeSpan.FromMilliseconds(20);
        public static TimeSpan WindowRestoreDelay => TimeSpan.FromMilliseconds(50);
        public static TimeSpan TimeUpdateInterval => TimeSpan.FromSeconds(1);
        public static TimeSpan TopmostEnforcementInterval => TimeSpan.FromSeconds(5);
        public static TimeSpan WindowMonitoringInterval => TimeSpan.FromMilliseconds(500);
        public static TimeSpan UpworkReadDelay => TimeSpan.FromSeconds(5);
        public static TimeSpan UpworkStaleValueRetryDelay => TimeSpan.FromSeconds(5);
        public static int UpworkStaleValueMaxRetries => 3;
    }

    public static class ConfirmationMessages
    {
        public static string ExitTitle => "Confirm Exit";
        public static string ExitMessage => "Are you sure you want to close the application?";
    }

    public static class Jira
    {
        public static string CloudInstanceUrl => "https://onestop.atlassian.net";
        public static string AuthorizationUrl => "https://auth.atlassian.com/authorize";
        public static string TokenUrl => "https://auth.atlassian.com/oauth/token";
        public static string CallbackUrl => "http://localhost:8080/callback";
        public static string Scopes => "read:jira-work write:jira-work offline_access";
        public static int MaxRetries => 3;

        // Cache settings
        public static TimeSpan CacheRefreshInterval => TimeSpan.FromMinutes(5);
        public static int CacheMaxIssuesPerQuery => 100;
        public static int SearchDebounceMs => 100;

        // Excluded statuses for cache (issues in these statuses won't be cached unless user is reporter/assignee)
        public static string[] ExcludedStatuses => new[] { "Cancelled", "Closed", "Done" };
    }

    public static class Icons
    {
        public static string PlayIcon => "M8,5 L8,19 L19,12 Z";
        // Stroke-only square (not filled)
        public static string StopIcon => "M6,6 L18,6 L18,18 L6,18 Z";
        // Two vertical bars for pause (stroke-only like stop icon)
        public static string PauseIcon => "M6,5 L6,19 L10,19 L10,5 Z M14,5 L14,19 L18,19 L18,5 Z";
    }

    public static class TimeTracking
    {
        public static TimeSpan LoggingBlockSize => TimeSpan.FromMinutes(10);
        public static TimeSpan SkipInitialOffsetThreshold => TimeSpan.FromSeconds(30);
        public static TimeSpan MinimumTimeToLogOnStop => TimeSpan.FromMinutes(5);
        public static TimeSpan MinimumTimeToLogOnChange => TimeSpan.FromMinutes(10);
        /// <summary>
        /// Minimum time to log in internal timer mode (no Upwork)
        /// </summary>
        public static TimeSpan MinimumTimeInternalMode => TimeSpan.FromMinutes(2);
        public static string TimeLogFile => "upwork.time.log";
    }

    public static class Deel
    {
        public static string ContractsUrl => "/time-attendance/contracts";
        public static string BaseUrl => "https://app.deel.com";

        public static class Timeouts
        {
            public static TimeSpan BrowserInitialization => TimeSpan.FromSeconds(30);
            public static TimeSpan ElementTimeout => TimeSpan.FromSeconds(10);
            public static TimeSpan PageSettleDownWait => TimeSpan.FromSeconds(5);
            public static TimeSpan ClientsideActionWait => TimeSpan.FromMilliseconds(500);
            public static TimeSpan ProfileCheckInterval => TimeSpan.FromMilliseconds(500);
        }

        public static class Selectors
        {
            public static string ProfileElement => "div.MuiBox-root[aria-label='Profile']";
        }
    }
}
