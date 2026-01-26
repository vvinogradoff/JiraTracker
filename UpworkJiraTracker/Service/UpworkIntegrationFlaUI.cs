using System.Diagnostics;
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

    // Regex pattern to match "40:30 hrs of 60 hrs" and extract "40:30"
    private static readonly Regex WeeklyTotalPattern = new(@"^(\d+:\d{2})\s+hrs\s+of\s+\d+\s+hrs$", RegexOptions.Compiled);

    public UpworkIntegrationFlaUI()
    {
        _automation = new UIA3Automation();
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
    /// Reads the weekly total time from Upwork Time Tracker window.
    /// Looks for element with Name like "40:30 hrs of 60 hrs" and extracts "40:30".
    /// </summary>
    /// <returns>Weekly total as TimeSpan, or null if not found</returns>
    public TimeSpan? ReadWeeklyTotal()
    {
        try
        {
            var processes = Process.GetProcessesByName(Constants.Upwork.ProcessName);
            if (processes.Length == 0) return null;

            var desktop = _automation.GetDesktop();

            foreach (var process in processes)
            {
                var windows = desktop.FindAllChildren(cf => cf.ByProcessId(process.Id));
                foreach (var window in windows)
                {
                    if (window.Name?.Equals(Constants.Upwork.WindowTitle, StringComparison.OrdinalIgnoreCase) != true)
                        continue;

                    // Search all elements for the weekly total pattern
                    var allElements = RecursivelyFindAllElements(window);
                    foreach (var element in allElements)
                    {
                        var name = element.Name;
                        if (string.IsNullOrEmpty(name)) continue;

                        var match = WeeklyTotalPattern.Match(name);
                        if (match.Success)
                        {
                            var timeStr = match.Groups[1].Value; // e.g., "40:30"
                            if (TryParseTime(timeStr, out var time))
                            {
                                Debug.WriteLine($"Found weekly total: {timeStr} -> {time}");
                                return time;
                            }
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading weekly total: {ex.Message}");
            return null;
        }
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

    public bool IsTracking()
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
