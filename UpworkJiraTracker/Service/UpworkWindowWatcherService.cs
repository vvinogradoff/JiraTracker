using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

using UpworkJiraTracker.Model;
using UpworkJiraTracker.Model.EventArgs;

namespace UpworkJiraTracker.Service;

/// <summary>
/// Monitors for new windows spawned by the Upwork process and triggers notifications.
/// Logs all window events to upwork.window.log for analysis.
/// </summary>
public class UpworkWindowWatcherService : IDisposable
{
	private static readonly TimeSpan PollInterval = Constants.Timeouts.WindowMonitoringInterval;
	private static readonly string LogFileName = Constants.TimeTracking.WindowLogFile;

	private readonly UIA3Automation _automation;
    private readonly DispatcherTimer _pollTimer;
    private readonly ConcurrentDictionary<nint, WindowInfo> _knownWindows = new();
    private bool _disposed = false;
    private bool _isRunning = false;

    public event EventHandler<WindowEventArgs>? WindowDetected;

    public UpworkWindowWatcherService()
    {
        _automation = new UIA3Automation();
        _pollTimer = new DispatcherTimer
        {
            Interval = PollInterval
        };
        _pollTimer.Tick += PollTimer_Tick;
    }

    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;
        _knownWindows.Clear();

        // Log all Upwork processes found
        var processes = Process.GetProcessesByName(Constants.Upwork.ProcessName);
        LogEvent("Watcher", $"Found {processes.Length} Upwork processes: [{string.Join(", ", processes.Select(p => $"PID={p.Id}"))}]");

        // Do initial scan to populate known windows (don't notify for existing)
        ScanWindows(notifyNewWindows: false);

        _pollTimer.Start();
        LogEvent("Watcher", $"Started monitoring - tracking {_knownWindows.Count} existing windows");
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _pollTimer.Stop();
        _isRunning = false;
        LogEvent("Watcher", "Stopped monitoring Upwork windows");
    }

    private void PollTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            ScanWindows(notifyNewWindows: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Window watcher poll error: {ex.Message}");
        }
    }

	private void ScanWindows(bool notifyNewWindows)
	{
		var processes = Process.GetProcessesByName(Constants.Upwork.ProcessName);
		if (processes.Length == 0)
		{
			// Upwork not running - clear known windows
			if (_knownWindows.Count > 0)
			{
				LogEvent("Watcher", "No Upwork processes found - clearing window cache");
				_knownWindows.Clear();
			}
			return;
		}

		// Build a set of all Upwork process IDs
		var upworkProcessIds = new HashSet<int>(processes.Select(p => p.Id));

		var currentWindowHandles = new HashSet<nint>();

		try
		{
			var desktop = _automation.GetDesktop();

			// Method 1: Scan ALL desktop children and filter by Upwork process IDs
			// This catches windows that might be owned by any Upwork subprocess
			var allDesktopWindows = desktop.FindAllChildren();

			foreach (var window in allDesktopWindows)
			{
				try
				{
					// Get the process ID of this window
					int windowProcessId = 0;
					try
					{
						windowProcessId = window.Properties.ProcessId.ValueOrDefault;
					}
					catch
					{
						continue; // Skip if we can't get process ID
					}

					// Check if this window belongs to any Upwork process
					if (!upworkProcessIds.Contains(windowProcessId))
						continue;

					var handle = window.Properties.NativeWindowHandle.ValueOrDefault;
					if (handle == IntPtr.Zero) continue;

					currentWindowHandles.Add(handle);

					var windowInfo = new WindowInfo
					{
						Handle = handle,
						Name = window.Name ?? "(no name)",
						ClassName = window.ClassName ?? "(no class)",
						ControlType = window.ControlType.ToString(),
						ProcessId = windowProcessId,
						BoundingRectangle = window.BoundingRectangle.ToString(),
						//AutomationId = window.AutomationId ?? "",
						FirstDetected = DateTime.Now
					};

					// Try to get additional info
					try
					{
						var children = window.FindAllChildren();
						windowInfo.ChildCount = children.Length;
					}
					catch
					{
						windowInfo.ChildCount = -1;
					}

					if (_knownWindows.TryAdd(handle, windowInfo))
					{
						if (notifyNewWindows)
							OnNewWindowDetected(windowInfo, true);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Error processing window: {ex.Message}");
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error scanning windows: {ex.Message}");
			LogEvent("Error", $"Scan failed: {ex.Message}");
		}

		// Remove windows that no longer exist
		var closedWindows = _knownWindows.Keys.Where(h => !currentWindowHandles.Contains(h)).ToList();
		foreach (var handle in closedWindows)
		{
			if (_knownWindows.TryRemove(handle, out var closedWindow))
				OnNewWindowDetected(closedWindow, isOpen: false);
		}
	}

    private void OnNewWindowDetected(WindowInfo windowInfo, bool isOpen)
    {
        // Raise event for external handlers
        WindowDetected?.Invoke(this, new WindowEventArgs(windowInfo, isOpen));
    }

    private void LogEvent(string eventType, string message)
    {
        var logDirectory = Properties.Settings.Default.LogDirectory;
        if (string.IsNullOrWhiteSpace(logDirectory))
            return;

        try
        {
            var directory = logDirectory == "."
                ? AppDomain.CurrentDomain.BaseDirectory
                : logDirectory;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var logPath = Path.Combine(directory, LogFileName);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            using var writer = new StreamWriter(logPath, append: true);
            writer.WriteLine($"{timestamp} [{eventType}] {message}");
            writer.Flush();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to write window log: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _automation?.Dispose();
            _disposed = true;
        }
    }
}
