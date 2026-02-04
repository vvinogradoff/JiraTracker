using System.Globalization;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using UpworkJiraTracker.Model;

namespace UpworkJiraTracker.Service;

/// <summary>
/// Service for logging time entries to both text and XLSX files
/// </summary>
public class TimeLogService
{
    private readonly object _lock = new();
    private string? _logDirectory;

    // Cache for loaded data
    private List<TimeLogWeek>? _cachedWeeks = null;
    private readonly object _cacheLock = new();

    public TimeLogService()
    {
        // Log directory will be set from settings
        _logDirectory = Properties.Settings.Default.LogDirectory;
        System.Diagnostics.Debug.WriteLine($"[TimeLogService] Constructor: LogDirectory setting = '{_logDirectory}'");
        System.Diagnostics.Debug.WriteLine($"[TimeLogService] Constructor: Resolved path = '{GetTimeLogFilePath()}'");
        System.Diagnostics.Debug.WriteLine($"[TimeLogService] Constructor: File exists = {System.IO.File.Exists(GetTimeLogFilePath())}");
        EnsureFileExists();
    }

    public void UpdateLogDirectory(string? logDirectory)
    {
        _logDirectory = logDirectory;
        InvalidateCache();
        EnsureFileExists();
    }

    /// <summary>
    /// Ensures the Excel file exists, creates it if it doesn't
    /// </summary>
    public void EnsureFileExists()
    {
        lock (_lock)
        {
            try
            {
                var filePath = GetTimeLogFilePath();
                if (!File.Exists(filePath))
                {
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("Time Log");
                    InitializeWorksheet(worksheet);
                    workbook.SaveAs(filePath);
                    workbook.Dispose();

                    System.Diagnostics.Debug.WriteLine($"Created new time log file at {filePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to ensure file exists: {ex.Message}");
            }
        }
    }

    private string GetLogDirectory()
    {
        return _logDirectory ?? Environment.CurrentDirectory;
    }

    public string GetTimeLogFilePath()
    {
        return Path.Combine(GetLogDirectory(), "TimeLog.xlsx");
    }

    private string GetTextLogFilePath()
    {
        return Path.Combine(GetLogDirectory(), "timelog.txt");
    }

    public void LogTime(string issueKey, string? summary, string? assignee, string? status, TimeSpan timeLogged, string? workDescription = null, double? remainingEstimateHours = null)
    {
        var timestamp = DateTime.Now;

        // Build remaining estimate string
        var remainingStr = remainingEstimateHours.HasValue
            ? FormatHoursAsTimeSpan(remainingEstimateHours.Value)
            : "N/A";

        // Log to text file with work description and remaining estimate
        var descPart = !string.IsNullOrWhiteSpace(workDescription) ? $", Description: {workDescription}" : "";
        LogToTextFile($"[{timestamp:yyyy-MM-dd HH:mm:ss}] TIME LOGGED - Issue: {issueKey}, Summary: {summary ?? "N/A"}, Assignee: {assignee ?? "N/A"}, Status: {status ?? "N/A"}, Time: {FormatTimeSpan(timeLogged)}, Remaining: {remainingStr}{descPart}");

        // Log to XLSX file
        LogToXlsx(issueKey, summary, timeLogged, timestamp);

        // Invalidate cache since data changed
        InvalidateCache();
    }

    private static string FormatHoursAsTimeSpan(double hours)
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

    public void LogStart(string issueKey, string? summary, string? assignee, string? status)
    {
        var timestamp = DateTime.Now;
        LogToTextFile($"[{timestamp:yyyy-MM-dd HH:mm:ss}] START - Issue: {issueKey}, Summary: {summary ?? "N/A"}, Assignee: {assignee ?? "N/A"}, Status: {status ?? "N/A"}");
    }

    public void LogSwitch(string issueKey, string? summary, string? assignee, string? status)
    {
        var timestamp = DateTime.Now;
        LogToTextFile($"[{timestamp:yyyy-MM-dd HH:mm:ss}] SWITCH - Issue: {issueKey}, Summary: {summary ?? "N/A"}, Assignee: {assignee ?? "N/A"}, Status: {status ?? "N/A"}");
    }

    public void LogStop(string issueKey, string? summary, string? assignee, string? status)
    {
        var timestamp = DateTime.Now;
        LogToTextFile($"[{timestamp:yyyy-MM-dd HH:mm:ss}] STOP - Issue: {issueKey}, Summary: {summary ?? "N/A"}, Assignee: {assignee ?? "N/A"}, Status: {status ?? "N/A"}");
    }

    private void LogToTextFile(string message)
    {
        if (string.IsNullOrWhiteSpace(_logDirectory) || _logDirectory == ".")
            return;

        lock (_lock)
        {
            try
            {
                var logPath = GetTextLogFilePath();
                var directory = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(logPath, message + Environment.NewLine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write to text log: {ex.Message}");
            }
        }
    }

    private void LogToXlsx(string issueKey, string? summary, TimeSpan timeLogged, DateTime timestamp)
    {
        lock (_lock)
        {
            try
            {
                var filePath = GetTimeLogFilePath();
                var (weekStart, weekEnd) = GetWeekRange(timestamp);
                var dayOfWeek = timestamp.DayOfWeek;

                IXLWorkbook workbook;
                IXLWorksheet worksheet;

                // Load or create workbook
                if (File.Exists(filePath))
                {
                    workbook = new XLWorkbook(filePath);
                    worksheet = workbook.Worksheets.FirstOrDefault() ?? workbook.Worksheets.Add("Time Log");
                }
                else
                {
                    // Ensure directory exists
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    workbook = new XLWorkbook();
                    worksheet = workbook.Worksheets.Add("Time Log");
                    InitializeWorksheet(worksheet);
                }

                // Find or create week section
                var sectionRow = FindWeekSection(worksheet, weekStart, weekEnd);
                if (sectionRow == -1)
                {
                    sectionRow = CreateWeekSection(worksheet, weekStart, weekEnd);
                }

                // Find or create row for this issue within the week section
                var issueRow = FindIssueRow(worksheet, sectionRow, issueKey);
                if (issueRow == -1)
                {
                    issueRow = CreateIssueRow(worksheet, sectionRow, issueKey, summary);
                }

                // Update the appropriate day column
                var dayColumn = GetDayColumn(dayOfWeek);
                var cell = worksheet.Cell(issueRow, dayColumn);
                var currentValue = cell.Value.IsNumber ? cell.Value.GetNumber() : 0.0;
                var newValue = currentValue + timeLogged.TotalHours;
                cell.Value = newValue;
                cell.Style.NumberFormat.Format = "0.00";

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Save workbook
                workbook.SaveAs(filePath);
                workbook.Dispose();

                System.Diagnostics.Debug.WriteLine($"Logged {timeLogged.TotalHours:F2}h to {issueKey} in XLSX on {timestamp:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to log time to XLSX: {ex.Message}");
            }
        }
    }

    private void InitializeWorksheet(IXLWorksheet worksheet)
    {
        // Header row
        worksheet.Cell(1, 1).Value = "Ticket ID";
        worksheet.Cell(1, 2).Value = "Summary";
        worksheet.Cell(1, 3).Value = "Mon";
        worksheet.Cell(1, 4).Value = "Tue";
        worksheet.Cell(1, 5).Value = "Wed";
        worksheet.Cell(1, 6).Value = "Thu";
        worksheet.Cell(1, 7).Value = "Fri";
        worksheet.Cell(1, 8).Value = "Sat";
        worksheet.Cell(1, 9).Value = "Sun";

        // Style header
        var headerRange = worksheet.Range(1, 1, 1, 9);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private (DateTime weekStart, DateTime weekEnd) GetWeekRange(DateTime date)
    {
        // Get Monday of the week
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        var monday = date.AddDays(-1 * diff).Date;
        var sunday = monday.AddDays(6);

        return (monday, sunday);
    }

    private int FindWeekSection(IXLWorksheet worksheet, DateTime weekStart, DateTime weekEnd)
    {
        var sectionHeader = FormatWeekRange(weekStart, weekEnd);
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        for (int row = 1; row <= lastRow; row++)
        {
            var cellValue = worksheet.Cell(row, 1).GetString();
            if (cellValue == sectionHeader)
            {
                return row;
            }
        }

        return -1;
    }

    private int CreateWeekSection(IXLWorksheet worksheet, DateTime weekStart, DateTime weekEnd)
    {
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        var sectionRow = lastRow + 2; // Leave a blank row

        // Section header
        var sectionHeader = FormatWeekRange(weekStart, weekEnd);
        worksheet.Cell(sectionRow, 1).Value = sectionHeader;
        worksheet.Cell(sectionRow, 1).Style.Font.Bold = true;
        worksheet.Cell(sectionRow, 1).Style.Font.FontSize = 12;

        return sectionRow;
    }

    private string FormatWeekRange(DateTime weekStart, DateTime weekEnd)
    {
        return $"{weekStart:MMM dd} - {weekEnd:MMM dd}";
    }

    private int FindIssueRow(IXLWorksheet worksheet, int sectionRow, string issueKey)
    {
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? sectionRow;

        // Search from section row + 1 until we hit another section or end
        for (int row = sectionRow + 1; row <= lastRow; row++)
        {
            var cellValue = worksheet.Cell(row, 1).GetString();

            // Stop if we hit another section (bold font indicates section header)
            if (worksheet.Cell(row, 1).Style.Font.Bold && row != sectionRow)
            {
                break;
            }

            if (cellValue == issueKey)
            {
                return row;
            }
        }

        return -1;
    }

    private int CreateIssueRow(IXLWorksheet worksheet, int sectionRow, string issueKey, string? summary)
    {
        // Find where to insert (right after section header)
        var insertRow = sectionRow + 1;

        // Check if there are already rows after the section header
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? sectionRow;
        for (int row = sectionRow + 1; row <= lastRow; row++)
        {
            var cellValue = worksheet.Cell(row, 1).GetString();

            // Stop if we hit another section
            if (worksheet.Cell(row, 1).Style.Font.Bold && row != sectionRow)
            {
                insertRow = row;
                break;
            }

            insertRow = row + 1;
        }

        // Insert new row
        worksheet.Cell(insertRow, 1).Value = issueKey;
        worksheet.Cell(insertRow, 2).Value = summary ?? "";

        return insertRow;
    }

    private int GetDayColumn(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => 3,
            DayOfWeek.Tuesday => 4,
            DayOfWeek.Wednesday => 5,
            DayOfWeek.Thursday => 6,
            DayOfWeek.Friday => 7,
            DayOfWeek.Saturday => 8,
            DayOfWeek.Sunday => 9,
            _ => 3
        };
    }

    private string FormatTimeSpan(TimeSpan time)
    {
        var hours = (int)time.TotalHours;
        var minutes = time.Minutes;

        if (hours > 0 && minutes > 0)
            return $"{hours}h {minutes}m";
        if (hours > 0)
            return $"{hours}h";
        return $"{minutes}m";
    }

    /// <summary>
    /// Loads all time log data from the Excel file
    /// </summary>
    public List<TimeLogWeek> LoadAllData()
    {
        lock (_lock)
        {
            var weeks = new List<TimeLogWeek>();

            try
            {
                var filePath = GetTimeLogFilePath();
                System.Diagnostics.Debug.WriteLine($"[TimeLogService] LoadAllData: Reading from {filePath}");

                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[TimeLogService] LoadAllData: File does not exist");
                    return weeks;
                }

                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[TimeLogService] LoadAllData: No worksheet found");
                    return weeks;
                }

                var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                TimeLogWeek? currentWeek = null;

                for (int row = 2; row <= lastRow; row++) // Skip header row
                {
                    var ticketId = worksheet.Cell(row, 1).GetString();
                    if (string.IsNullOrWhiteSpace(ticketId))
                        continue;

                    // Check if this is a week header (bold font and contains " - " which is the week range separator)
                    // Week headers look like "Jan 19 - Jan 25" (with spaces around dash)
                    // Ticket IDs look like "DTM-4925" (no spaces around dash)
                    if (worksheet.Cell(row, 1).Style.Font.Bold && ticketId.Contains(" - "))
                    {
                        // Parse week range: "Jan 19 - Jan 25"
                        var (weekStart, weekEnd) = ParseWeekRange(ticketId);
                        System.Diagnostics.Debug.WriteLine($"[TimeLogService] Found week header: '{ticketId}' -> {weekStart:yyyy-MM-dd} to {weekEnd:yyyy-MM-dd}");
                        currentWeek = new TimeLogWeek
                        {
                            WeekStart = weekStart,
                            WeekEnd = weekEnd
                        };
                        weeks.Add(currentWeek);
                        continue;
                    }

                    // This is a ticket entry
                    if (currentWeek != null)
                    {
                        var entry = new TimeLogEntry
                        {
                            TicketId = ticketId,
                            Summary = worksheet.Cell(row, 2).GetString()
                        };

                        // Read daily hours (columns 3-9 are Mon-Sun)
                        for (int col = 3; col <= 9; col++)
                        {
                            var hours = worksheet.Cell(row, col).Value.IsNumber
                                ? worksheet.Cell(row, col).Value.GetNumber()
                                : 0.0;

                            if (hours > 0)
                            {
                                var day = GetDayFromColumn(col);
                                entry.DailyHours[day] = hours;
                            }
                        }

                        if (entry.TotalHours > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[TimeLogService] Found entry: {ticketId} with {entry.TotalHours}h total");
                        }
                        currentWeek.Entries.Add(entry);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TimeLogService] LoadAllData: EXCEPTION - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TimeLogService] LoadAllData: Stack trace - {ex.StackTrace}");
            }

            System.Diagnostics.Debug.WriteLine($"[TimeLogService] LoadAllData: Returning {weeks.Count} weeks with {weeks.Sum(w => w.Entries.Count)} total entries");
            return weeks;
        }
    }

    /// <summary>
    /// Calculates time statistics for today, this week, and this month
    /// </summary>
    public TimeStats CalculateStats()
    {
        var stats = new TimeStats();
        var weeks = LoadAllData();
        var now = DateTime.Now.Date;

        var (currentWeekStart, _) = GetWeekRange(now);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        foreach (var week in weeks)
        {
            foreach (var entry in week.Entries)
            {
                foreach (var (day, hours) in entry.DailyHours)
                {
                    // Calculate the actual date for this day
                    var dayDate = GetDateForDay(week.WeekStart, day);

                    // Today
                    if (dayDate == now)
                    {
                        stats.TodayHours += hours;
                    }

                    // This week
                    if (dayDate >= currentWeekStart && dayDate <= now)
                    {
                        stats.ThisWeekHours += hours;
                    }

                    // This month
                    if (dayDate >= monthStart && dayDate <= now)
                    {
                        stats.ThisMonthHours += hours;
                    }
                }
            }
        }

        return stats;
    }

    /// <summary>
    /// Invalidates the cached time log data, forcing next load to read from disk
    /// </summary>
    public void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cachedWeeks = null;
        }
    }

    /// <summary>
    /// Asynchronously loads all time log data from the Excel file
    /// </summary>
    public async Task<List<TimeLogWeek>> LoadAllDataAsync()
    {
        // Check cache first
        lock (_cacheLock)
        {
            if (_cachedWeeks != null)
            {
                return _cachedWeeks;
            }
        }

        // Load from disk on background thread
        var weeks = await Task.Run(() => LoadAllData());

        // Cache the result
        lock (_cacheLock)
        {
            _cachedWeeks = weeks;
        }

        return weeks;
    }

    /// <summary>
    /// Asynchronously calculates time statistics for today, this week, and this month
    /// </summary>
    public async Task<TimeStats> CalculateStatsAsync()
    {
        var weeks = await LoadAllDataAsync();
        var stats = new TimeStats();
        var now = DateTime.Now.Date;

        System.Diagnostics.Debug.WriteLine($"[TimeLogService] CalculateStatsAsync: Loaded {weeks.Count} weeks, Today is {now:yyyy-MM-dd}");

        var (currentWeekStart, _) = GetWeekRange(now);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        System.Diagnostics.Debug.WriteLine($"[TimeLogService] Current week starts: {currentWeekStart:yyyy-MM-dd}, Month starts: {monthStart:yyyy-MM-dd}");

        foreach (var week in weeks)
        {
            System.Diagnostics.Debug.WriteLine($"[TimeLogService] Processing week {week.WeekLabel} ({week.WeekStart:yyyy-MM-dd} to {week.WeekEnd:yyyy-MM-dd}) with {week.Entries.Count} entries");

            foreach (var entry in week.Entries)
            {
                foreach (var (day, hours) in entry.DailyHours)
                {
                    // Calculate the actual date for this day
                    var dayDate = GetDateForDay(week.WeekStart, day);

                    System.Diagnostics.Debug.WriteLine($"[TimeLogService]   {entry.TicketId} on {day} ({dayDate:yyyy-MM-dd}): {hours}h");

                    // Today
                    if (dayDate == now)
                    {
                        stats.TodayHours += hours;
                        System.Diagnostics.Debug.WriteLine($"[TimeLogService]     -> Added to TODAY");
                    }

                    // This week
                    if (dayDate >= currentWeekStart && dayDate <= now)
                    {
                        stats.ThisWeekHours += hours;
                        System.Diagnostics.Debug.WriteLine($"[TimeLogService]     -> Added to THIS WEEK");
                    }

                    // This month
                    if (dayDate >= monthStart && dayDate <= now)
                    {
                        stats.ThisMonthHours += hours;
                        System.Diagnostics.Debug.WriteLine($"[TimeLogService]     -> Added to THIS MONTH");
                    }
                }
            }
        }

        System.Diagnostics.Debug.WriteLine($"[TimeLogService] Final stats: Today={stats.TodayHours}h, Week={stats.ThisWeekHours}h, Month={stats.ThisMonthHours}h");

        return stats;
    }

    private (DateTime weekStart, DateTime weekEnd) ParseWeekRange(string weekLabel)
    {
        try
        {
            // Format: "Jan 19 - Jan 25" or "Dec 26 - Jan 01"
            var parts = weekLabel.Split(new[] { " - " }, StringSplitOptions.None);
            if (parts.Length != 2)
                return (DateTime.MinValue, DateTime.MinValue);

            var now = DateTime.Now;
            var currentYear = now.Year;

            // Parse start date
            var startParts = parts[0].Trim().Split(' ');
            var startMonth = DateTime.ParseExact(startParts[0], "MMM", CultureInfo.InvariantCulture).Month;
            var startDay = int.Parse(startParts[1]);

            // Parse end date
            var endParts = parts[1].Trim().Split(' ');
            var endMonth = DateTime.ParseExact(endParts[0], "MMM", CultureInfo.InvariantCulture).Month;
            var endDay = int.Parse(endParts[1]);

            // Determine the correct year
            var startYear = currentYear;
            var endYear = currentYear;

            // Check if week spans year boundary (Dec -> Jan)
            if (startMonth == 12 && endMonth == 1)
            {
                // Could be Dec currentYear -> Jan nextYear, OR Dec lastYear -> Jan currentYear
                // Use heuristic: if Dec is more than 6 months in the future, it's probably last year
                var candidateStart = new DateTime(currentYear, startMonth, startDay);
                if (candidateStart > now.AddMonths(6))
                {
                    // Dec is far in the future - this must be from last year
                    startYear = currentYear - 1;
                    endYear = currentYear;
                }
                else
                {
                    endYear = currentYear + 1;
                }
            }
            else if (startMonth > endMonth)
            {
                // Invalid case (shouldn't happen with proper data)
                startYear = currentYear - 1;
            }
            else
            {
                // Normal case - both in same year
                // If the week is more than 6 months in the future, assume it's from last year
                var candidateStart = new DateTime(currentYear, startMonth, startDay);
                if (candidateStart > now.AddMonths(6))
                {
                    startYear = currentYear - 1;
                    endYear = currentYear - 1;
                }
            }

            var weekStart = new DateTime(startYear, startMonth, startDay);
            var weekEnd = new DateTime(endYear, endMonth, endDay);

            return (weekStart, weekEnd);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TimeLogService] ParseWeekRange failed for '{weekLabel}': {ex.Message}");
            return (DateTime.MinValue, DateTime.MinValue);
        }
    }

    private DayOfWeek GetDayFromColumn(int column)
    {
        return column switch
        {
            3 => DayOfWeek.Monday,
            4 => DayOfWeek.Tuesday,
            5 => DayOfWeek.Wednesday,
            6 => DayOfWeek.Thursday,
            7 => DayOfWeek.Friday,
            8 => DayOfWeek.Saturday,
            9 => DayOfWeek.Sunday,
            _ => DayOfWeek.Monday
        };
    }

    private DateTime GetDateForDay(DateTime weekStart, DayOfWeek targetDay)
    {
        var daysToAdd = ((int)targetDay - (int)weekStart.DayOfWeek + 7) % 7;
        return weekStart.AddDays(daysToAdd);
    }
}
