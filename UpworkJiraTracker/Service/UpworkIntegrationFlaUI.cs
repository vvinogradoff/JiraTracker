using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;

namespace UpworkJiraTracker.Service;

public class UpworkIntegrationFlaUI : IDisposable
{
    private readonly UIA3Automation _automation;
    private static readonly string DiagLogFileName = Constants.TimeTracking.WindowLogFile;

    // Regex pattern to match "40:30 hrs of 60 hrs" and extract "40:30"
    private static readonly Regex WeeklyTotalPattern = new(@"^(\d+:\d{2})\s+hrs\s+of\s+\d+\s+hrs$", RegexOptions.Compiled);

    // Regex pattern to match "h:mm hrs" format (e.g., "0:00 hrs", "1:23 hrs")
    private static readonly Regex TimeHrsPattern = new(@"^\d+:\d{2}\s+hrs$", RegexOptions.Compiled);

    // Regex pattern to match "xxxxx ago" format (e.g., "11 hours ago", "32 seconds ago")
    private static readonly Regex AgoPattern = new(@".+\s+ago$", RegexOptions.Compiled);

    public UpworkIntegrationFlaUI()
    {
        _automation = new UIA3Automation();
    }

    /// <summary>
    /// Diagnostic method that dumps information about ALL Upwork processes and windows.
    /// Logs to upwork.window.log for analysis.
    /// </summary>
    public async Task DumpAllUpworkWindowsDiagnostics()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== UPWORK DIAGNOSTICS START ===");
        sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

        try
        {
            var processes = Process.GetProcessesByName(Constants.Upwork.ProcessName);
            sb.AppendLine($"Found {processes.Length} Upwork process(es)");

            if (processes.Length == 0)
            {
                sb.AppendLine("No Upwork processes found.");
                LogDiagnostics(sb.ToString());
                return;
            }

            // Log process info
            foreach (var process in processes)
            {
                try
                {
                    sb.AppendLine($"  Process PID={process.Id}, Memory={process.WorkingSet64 / 1024 / 1024}MB, StartTime={process.StartTime:HH:mm:ss}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"  Process PID={process.Id}, (could not read details: {ex.Message})");
                }
            }

            sb.AppendLine();

            var desktop = _automation.GetDesktop();
            int totalWindows = 0;
            int timeTrackerWindows = 0;

            foreach (var process in processes)
            {
                sb.AppendLine($"--- Process PID={process.Id} Windows ---");

                var windows = await Task.Run(() => desktop.FindAllChildren(cf => cf.ByProcessId(process.Id)));
                sb.AppendLine($"  Found {windows.Length} window(s) for this process");

                foreach (var window in windows)
                {
                    totalWindows++;
                    try
                    {
                        var windowName = window.Name ?? "(null)";
                        var windowClass = window.ClassName ?? "(null)";
                        var handle = window.Properties.NativeWindowHandle.ValueOrDefault;
                        var bounds = window.BoundingRectangle;

                        sb.AppendLine($"  Window: \"{windowName}\"");
                        sb.AppendLine($"    Handle: {handle}");
                        sb.AppendLine($"    Class: {windowClass}");
                        sb.AppendLine($"    Bounds: {bounds}");

                        // Check if this is a Time Tracker window
                        if (windowName.Equals(Constants.Upwork.WindowTitle, StringComparison.OrdinalIgnoreCase))
                        {
                            timeTrackerWindows++;
                            sb.AppendLine($"    >>> THIS IS A TIME TRACKER WINDOW (#{timeTrackerWindows}) <<<");

                            // Try to read time data from this window
                            await DumpTimeDataFromWindow(window, sb);
                        }
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"    Error reading window: {ex.Message}");
                    }
                }

                sb.AppendLine();
            }

            sb.AppendLine($"SUMMARY: {processes.Length} processes, {totalWindows} total windows, {timeTrackerWindows} Time Tracker windows");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"DIAGNOSTIC ERROR: {ex.Message}");
            sb.AppendLine(ex.StackTrace);
        }

        sb.AppendLine("=== UPWORK DIAGNOSTICS END ===");
        sb.AppendLine();

        LogDiagnostics(sb.ToString());
    }

    private async Task DumpTimeDataFromWindow(AutomationElement window, StringBuilder sb)
    {
        try
        {
            var elements = await Task.Run(() => RecursivelyFindAllElements(window));
            sb.AppendLine($"    Element count: {elements.Count}");

            // Collect all time-related values
            var timeValues = new List<string>();
            string? weeklyTotal = null;
            string? currentSession = null;
            string? todayTime = null;
            string? lastScreenshot = null;

            foreach (var element in elements)
            {
                var name = element.Name;
                if (string.IsNullOrEmpty(name)) continue;

                // Check for weekly total pattern
                if (WeeklyTotalPattern.IsMatch(name))
                {
                    weeklyTotal = name;
                    timeValues.Add($"WeeklyTotal: {name}");
                    continue;
                }

                // Check for "ago" pattern
                if (AgoPattern.IsMatch(name))
                {
                    lastScreenshot = name;
                    timeValues.Add($"LastScreenshot: {name}");
                    continue;
                }

                // Check for time hrs pattern
                if (TimeHrsPattern.IsMatch(name))
                {
                    try
                    {
                        var className = element.ClassName;
                        if (!string.IsNullOrEmpty(className) && className.Contains("h1") && className.Contains("m-0"))
                        {
                            currentSession = name;
                            timeValues.Add($"CurrentSession: {name} (class={className})");
                        }
                        else if (string.IsNullOrEmpty(className))
                        {
                            todayTime = name;
                            timeValues.Add($"TodayTime: {name}");
                        }
                        else
                        {
                            timeValues.Add($"OtherTime: {name} (class={className})");
                        }
                    }
                    catch
                    {
                        timeValues.Add($"Time: {name} (class=unknown)");
                    }
                }
            }

            sb.AppendLine($"    Time values found:");
            if (timeValues.Count == 0)
            {
                sb.AppendLine($"      (none)");
            }
            else
            {
                foreach (var tv in timeValues)
                {
                    sb.AppendLine($"      {tv}");
                }
            }

            // Summary
            sb.AppendLine($"    Parsed: Weekly={weeklyTotal ?? "?"}, Session={currentSession ?? "?"}, Today={todayTime ?? "?"}, Screenshot={lastScreenshot ?? "?"}");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"    Error reading time data: {ex.Message}");
        }
    }

    private void LogDiagnostics(string message)
    {
        var logDirectory = Properties.Settings.Default.LogDirectory;
        if (string.IsNullOrWhiteSpace(logDirectory))
        {
            Debug.WriteLine("Diagnostics (no log directory configured):");
            Debug.WriteLine(message);
            return;
        }

        try
        {
            var directory = logDirectory == "."
                ? AppDomain.CurrentDomain.BaseDirectory
                : logDirectory;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var logPath = Path.Combine(directory, DiagLogFileName);

            using var writer = new StreamWriter(logPath, append: true);
            writer.Write(message);
            writer.Flush();

            Debug.WriteLine($"Diagnostics written to {logPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to write diagnostics: {ex.Message}");
            Debug.WriteLine(message);
        }
    }

    /// <summary>
    /// Checks if Upwork Time Tracker window is available (without restoring/focusing)
    /// </summary>
    public bool IsUpworkAvailable()
    {
        try
        {
            var processes = Process.GetProcessesByName(Constants.Upwork.ProcessName);
            if (processes.Length == 0) return false;

            var desktop = _automation.GetDesktop();

            foreach (var process in processes)
            {
                var windows = desktop.FindAllChildren(cf => cf.ByProcessId(process.Id));
                foreach (var window in windows)
                {
                    if (window.Name?.Equals(Constants.Upwork.WindowTitle, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Reads all time statistics from Upwork Time Tracker window.
    /// Includes current session, today's time, weekly total, and latest screenshot time.
    /// Also parses and returns the weekly total as TimeSpan for convenience.
    /// </summary>
    /// <returns>Tuple of (UpworkTimeStats, WeeklyTotal as TimeSpan)</returns>
    public async Task<(Model.UpworkTimeStats? Stats, TimeSpan? WeeklyTotal)> ReadAllTimeData()
    {
        try
        {
            var elements = await GetTimeTrackerElements();
            if (elements == null) return (null, null);

            var stats = new Model.UpworkTimeStats();
            TimeSpan? weeklyTotal = null;

            foreach (var element in elements)
            {
                var name = element.Name;
                if (string.IsNullOrEmpty(name)) continue;

                // Check for weekly total pattern ("40:30 hrs of 60 hrs")
                var weeklyMatch = WeeklyTotalPattern.Match(name);
                if (weeklyMatch.Success)
                {
                    stats.WeeklyTotal = name;
                    var timeStr = weeklyMatch.Groups[1].Value;
                    if (TryParseTime(timeStr, out var time))
                    {
                        weeklyTotal = time;
                        Debug.WriteLine($"Found weekly total: {timeStr} -> {time}");
                    }
                    continue;
                }

                // Check for "xxxxx ago" pattern (latest screenshot time)
                if (AgoPattern.IsMatch(name))
                {
                    stats.LatestScreenshotTime = name;
                    continue;
                }

                // Check for "h:mm hrs" pattern
                if (TimeHrsPattern.IsMatch(name))
                {
                    try
                    {
                        var className = element.ClassName;

                        // Current session time has Class="h1 m-0"
                        if (!string.IsNullOrEmpty(className) && className.Contains("h1") && className.Contains("m-0"))
                        {
                            stats.CurrentSessionTime = name;
                        }
                        // Today's time has no class or empty class
                        else if (string.IsNullOrEmpty(className))
                        {
                            stats.TodayTime = name;
                        }
                    }
                    catch
                    {
                        // If we can't access ClassName, skip this element
                    }
                }
            }

            return (stats, weeklyTotal);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading time data: {ex.Message}");
            return (null, null);
        }
    }

    /// <summary>
    /// Reads all time statistics from Upwork Time Tracker window.
    /// Includes current session, today's time, weekly total, and latest screenshot time.
    /// </summary>
    /// <returns>UpworkTimeStats object with all available statistics</returns>
    public async Task<Model.UpworkTimeStats?> ReadTimeStats()
    {
        var (stats, _) = await ReadAllTimeData();
        return stats;
    }

    /// <summary>
    /// Reads the weekly total time from Upwork Time Tracker window.
    /// Looks for element with Name like "40:30 hrs of 60 hrs" and extracts "40:30".
    /// </summary>
    /// <returns>Weekly total as TimeSpan, or null if not found</returns>
    public async Task<TimeSpan?> ReadWeeklyTotal()
    {
        var (_, weeklyTotal) = await ReadAllTimeData();
        return weeklyTotal;
    }

    /// <summary>
    /// Gets all UI elements from the Time Tracker window.
    /// This is the shared enumeration method used by ReadAllTimeData.
    /// </summary>
    private async Task<List<AutomationElement>?> GetTimeTrackerElements()
    {
        var processes = Process.GetProcessesByName(Constants.Upwork.ProcessName);
        if (processes.Length == 0) return null;

        foreach (var process in processes)
        {
            var windows = await FindWindowsOfProcess(process);
            foreach (var window in windows)
            {
                if (window.Name?.Equals(Constants.Upwork.WindowTitle, StringComparison.OrdinalIgnoreCase) != true)
                    continue;

                return RecursivelyFindAllElements(window);
            }
        }

        return null;
    }

    private static bool TryParseTime(string timeStr, out TimeSpan result)
    {
        result = TimeSpan.Zero;
        var parts = timeStr.Split(':');
        if (parts.Length != 2) return false;

        if (int.TryParse(parts[0], out var hours) && int.TryParse(parts[1], out var minutes))
        {
            result = new TimeSpan(hours, minutes, 0);
            return true;
        }

        return false;
    }

    public bool ClickStartTracking()
    {
        try
        {
            return ClickButton(Constants.Upwork.ButtonText.Start, Constants.Upwork.ButtonText.StartTracking);
        }
        catch
        {
            return false;
        }
    }

    public bool ClickStopTracking()
    {
        try
        {
            return ClickButton(Constants.Upwork.ButtonText.Stop, Constants.Upwork.ButtonText.StopTracking);
        }
        catch
        {
            return false;
        }
    }

    public bool ClickEditButton()
    {
        try
        {
            return ClickButton(Constants.Upwork.ButtonText.Edit);
        }
        catch
        {
            return false;
        }
    }

    public bool CheckIsTracking()
    {
        try
        {
            var window = FindAndRestoreTimeTrackerWindow();
            if (window == null) return false;

            var searchRoot = FindSearchRoot(window);
            var allElements = RecursivelyFindAllElements(searchRoot);

            // Check if "Stop" button exists (means tracking is active)
            foreach (var element in allElements)
            {
                var name = element.Name;
                if (string.IsNullOrEmpty(name)) continue;

                var automationId = element.AutomationId;
                if (automationId == Constants.Upwork.AutomationIds.RestoreButton ||
                    automationId == Constants.Upwork.AutomationIds.MaximizeButton ||
                    automationId == Constants.Upwork.AutomationIds.CloseButton)
                {
                    continue;
                }

                if (name.Contains(Constants.Upwork.ButtonText.Stop, StringComparison.OrdinalIgnoreCase) ||
                    name.Contains(Constants.Upwork.ButtonText.StopTracking, StringComparison.OrdinalIgnoreCase))
                {
                    // Minimize the window after checking
                    MinimizeWindow(window);
                    return true;
                }
            }

            // Minimize the window after checking
            MinimizeWindow(window);
            return false;
        }
        catch
        {
            return false;
        }
    }

    public bool UpdateMemo(string memoText)
    {
        try
        {
            var window = FindAndRestoreTimeTrackerWindow();
            if (window == null) return false;

        // First click Edit button
        if (!ClickEditButton())
        {
            return false;
        }

        Thread.Sleep(Constants.Timeouts.WindowRestoreDelay);

        var searchRoot = FindSearchRoot(window);
        var allElements = RecursivelyFindAllElements(searchRoot);

		// First click the field for edit to appear
		foreach (var element in allElements)
		{
			if (element.Name == Constants.Upwork.MemoFieldLabel && element.ControlType == ControlType.Button)
			{
				if (element.Patterns.Invoke.IsSupported)
					element.Patterns.Invoke.Pattern.Invoke();
				else
					element.Click();
			}
		}

		// repopulate updated elements list
		allElements = RecursivelyFindAllElements(searchRoot);

		AutomationElement? editControl = null;
        foreach (var element in allElements)
        {
            if (element.ControlType == ControlType.Edit || element.ControlType == ControlType.ComboBox)
            {
                editControl = element;
                break;
            }
        }

        if (editControl == null)
        {
            return false;
        }

        // Keyboard simulation requires focus on the target control
        editControl.Focus();
        Thread.Sleep(Constants.Timeouts.ShortDelay);

        Keyboard.Press(VirtualKeyShort.END);
        Thread.Sleep(Constants.Timeouts.ShortDelay);

        Keyboard.Press(VirtualKeyShort.CONTROL);
        Keyboard.Press(VirtualKeyShort.KEY_A);
        Keyboard.Release(VirtualKeyShort.CONTROL);
        Keyboard.Release(VirtualKeyShort.KEY_A);
        Thread.Sleep(Constants.Timeouts.ShortDelay);

        Keyboard.Press(VirtualKeyShort.DELETE);
        Thread.Sleep(Constants.Timeouts.ShortDelay);

        Keyboard.Type(memoText);
        Thread.Sleep(Constants.Timeouts.ShortDelay);

            var success = ClickButton(Constants.Upwork.ButtonText.StartTracking, Constants.Upwork.ButtonText.Update);

            if (success)
            {
                // Minimize the Upwork window
                MinimizeWindow(window);
            }

            return success;
        }
        catch
        {
            return false;
        }
    }

    private void MinimizeWindow(Window window)
    {
        try
        {
            var windowPattern = window.Patterns.Window;
            if (windowPattern.IsSupported)
            {
                windowPattern.Pattern.SetWindowVisualState(WindowVisualState.Minimized);
            }
        }
        catch
        {
            // Ignore errors minimizing window
        }
    }

    private bool ClickButton(params string[] buttonTexts)
    {
        var window = FindAndRestoreTimeTrackerWindow();
        if (window == null) return false;

        var searchRoot = FindSearchRoot(window);
        var allElements = RecursivelyFindAllElements(searchRoot);

        foreach (var element in allElements)
        {
            var name = element.Name;
            if (string.IsNullOrEmpty(name)) continue;

            foreach (var targetText in buttonTexts)
            {
                if (name.Contains(targetText, StringComparison.OrdinalIgnoreCase))
                {
                    var automationId = element.AutomationId;
                    if (automationId == Constants.Upwork.AutomationIds.RestoreButton ||
                        automationId == Constants.Upwork.AutomationIds.MaximizeButton ||
                        automationId == Constants.Upwork.AutomationIds.CloseButton)
                    {
                        continue;
                    }

                    if (element.Patterns.Invoke.IsSupported)
                    {
                        element.Patterns.Invoke.Pattern.Invoke();
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private Window? FindAndRestoreTimeTrackerWindow()
    {
        var processes = Process.GetProcessesByName(Constants.Upwork.ProcessName);
        if (processes.Length == 0) return null;

        var desktop = _automation.GetDesktop();
        var allWindows = desktop.FindAllChildren(cf => cf.ByProcessId(processes[0].Id));

        Window? timeTrackerWindow = null;

        foreach (var window in allWindows)
        {
            if (window.Name?.Equals(Constants.Upwork.WindowTitle, StringComparison.OrdinalIgnoreCase) == true)
            {
                timeTrackerWindow = window.AsWindow();
                break;
            }
        }

        if (timeTrackerWindow == null) return null;

        var windowPattern = timeTrackerWindow.Patterns.Window;
        if (windowPattern.IsSupported)
        {
            var currentState = windowPattern.Pattern.WindowVisualState;
            if (currentState == WindowVisualState.Minimized)
            {
                windowPattern.Pattern.SetWindowVisualState(WindowVisualState.Normal);
                Thread.Sleep(Constants.Timeouts.WindowRestoreDelay);
            }
        }

        if (Constants.Upwork.ForceFocusWindow)
        {
            timeTrackerWindow.Focus();
            Thread.Sleep(Constants.Timeouts.ShortDelay);
        }

        return timeTrackerWindow;
    }

	private Task<AutomationElement[]> FindWindowsOfProcess(Process process)
		=> Task.Run(() => _automation.GetDesktop().FindAllChildren(cf => cf.ByProcessId(process.Id)));

	private AutomationElement FindSearchRoot(Window window)
    {
        var documentElement = window.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Document).Or(cf.ByControlType(ControlType.Pane)));

        return documentElement ?? window;
    }

    private List<AutomationElement> RecursivelyFindAllElements(AutomationElement parent)
    {
        var result = new List<AutomationElement>();

        try
        {
            result.Add(parent);

            var children = parent.FindAllChildren();
            foreach (var child in children)
            {
                var childElements = RecursivelyFindAllElements(child);
                result.AddRange(childElements);
            }
        }
        catch
        {
            // Silently handle errors during recursion
        }

        return result;
    }

    public void Dispose()
    {
        _automation?.Dispose();
    }
}
