namespace UpworkJiraTracker.Model;

public class UpworkTimeStats
{
    /// <summary>
    /// Current session time (e.g., "0:00 hrs")
    /// Text with Class="h1 m-0"
    /// </summary>
    public string? CurrentSessionTime { get; set; }

    /// <summary>
    /// Today's total time (e.g., "0:00 hrs")
    /// Text with no Class attribute
    /// </summary>
    public string? TodayTime { get; set; }

    /// <summary>
    /// Latest screenshot time (e.g., "11 hours ago")
    /// Text ending with "ago"
    /// </summary>
    public string? LatestScreenshotTime { get; set; }

    /// <summary>
    /// Weekly total (e.g., "40:30 hrs of 60 hrs")
    /// </summary>
    public string? WeeklyTotal { get; set; }

    /// <summary>
    /// Gets a formatted multiline string for tooltip display
    /// </summary>
    public string ToTooltipString()
    {
        return $"Current Session: {CurrentSessionTime ?? "N/A"}\n" +
               $"Today: {TodayTime ?? "N/A"}\n" +
               $"Weekly: {WeeklyTotal ?? "N/A"}\n" +
               $"Screenshot: {LatestScreenshotTime ?? "N/A"}";
    }
}
