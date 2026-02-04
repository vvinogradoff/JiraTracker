namespace UpworkJiraTracker.Model;

/// <summary>
/// Represents a week's worth of time log data
/// </summary>
public class TimeLogWeek
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public List<TimeLogEntry> Entries { get; set; } = new();

    public string WeekLabel => $"{WeekStart:MMM dd} - {WeekEnd:MMM dd}";
}

/// <summary>
/// Represents a single time log entry for a ticket
/// </summary>
public class TimeLogEntry
{
    public string TicketId { get; set; } = "";
    public string? Summary { get; set; }
    public Dictionary<DayOfWeek, double> DailyHours { get; set; } = new();

    public double TotalHours => DailyHours.Values.Sum();

    public double GetHours(DayOfWeek day) => DailyHours.TryGetValue(day, out var hours) ? hours : 0.0;

    // Properties for each day to simplify XAML binding
    public double Monday => GetHours(DayOfWeek.Monday);
    public double Tuesday => GetHours(DayOfWeek.Tuesday);
    public double Wednesday => GetHours(DayOfWeek.Wednesday);
    public double Thursday => GetHours(DayOfWeek.Thursday);
    public double Friday => GetHours(DayOfWeek.Friday);
    public double Saturday => GetHours(DayOfWeek.Saturday);
    public double Sunday => GetHours(DayOfWeek.Sunday);
}

/// <summary>
/// Aggregated time statistics
/// </summary>
public class TimeStats
{
    public double TodayHours { get; set; }
    public double ThisWeekHours { get; set; }
    public double ThisMonthHours { get; set; }

    public string ToTooltipString()
    {
        return $"Today: {FormatHours(TodayHours)}\n" +
               $"This Week: {FormatHours(ThisWeekHours)}\n" +
               $"This Month: {FormatHours(ThisMonthHours)}";
    }

    private static string FormatHours(double hours)
    {
        var h = (int)hours;
        var m = (int)((hours - h) * 60);

        if (h > 0 && m > 0)
            return $"{h}h {m}m";
        if (h > 0)
            return $"{h}h";
        if (m > 0)
            return $"{m}m";
        return "0m";
    }
}
