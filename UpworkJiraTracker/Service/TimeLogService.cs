using System.IO;

namespace UpworkJiraTracker.Service;

public class TimeLogService
{
    private const string LogFileName = "timelog.txt";

    public void LogStart(string issueKey, string summary, string assignee, string status)
    {
        WriteLogEntry("Start", issueKey, summary, assignee, status);
    }

    public void LogStop(string issueKey, string summary, string assignee, string status)
    {
        WriteLogEntry("Stop", issueKey, summary, assignee, status);
    }

    public void LogSwitch(string issueKey, string summary, string assignee, string status)
    {
        WriteLogEntry("Switch", issueKey, summary, assignee, status);
    }

    public void LogTime(string issueKey, string summary, string assignee, string status, TimeSpan timeLogged)
    {
        var timeFormatted = FormatTimeLogged(timeLogged);
        WriteLogEntry("Time", issueKey, $"{timeFormatted} logged to {issueKey} {summary}", assignee, status);
    }

    private void WriteLogEntry(string eventType, string issueKey, string summary, string assignee, string status)
    {
        var logDirectory = Properties.Settings.Default.LogDirectory;

        // Skip logging if directory is empty
        if (string.IsNullOrWhiteSpace(logDirectory))
            return;

        try
        {
            // Resolve path (handle "." as current directory)
            var directory = logDirectory == "."
                ? AppDomain.CurrentDomain.BaseDirectory
                : logDirectory;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var logPath = Path.Combine(directory, LogFileName);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Format: "2021-01-22 12:25:41 [Start] DMT-1234 Jira ticket summary, Vitaly Vinogradov (In Progress)"
            string logLine;
            if (eventType == "Time")
            {
                // For Time events, summary already contains the full message
                logLine = $"{timestamp} [{eventType}] {summary}, {assignee} ({status})";
            }
            else
            {
                logLine = $"{timestamp} [{eventType}] {issueKey} {summary}, {assignee} ({status})";
            }

            // Write and flush immediately
            using var writer = new StreamWriter(logPath, append: true);
            writer.WriteLine(logLine);
            writer.Flush();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to write time log: {ex.Message}");
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
}
