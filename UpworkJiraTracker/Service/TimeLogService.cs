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

        // Log to XLSX file (prefer workDescription, fall back to summary)
        LogToXlsx(issueKey, workDescription ?? summary, timeLogged, timestamp);

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

    public void LogPause(string issueKey, string? summary, string? assignee, string? status)
    {
        var timestamp = DateTime.Now;
        LogToTextFile($"[{timestamp:yyyy-MM-dd HH:mm:ss}] PAUSE - Issue: {issueKey}, Summary: {summary ?? "N/A"}, Assignee: {assignee ?? "N/A"}, Status: {status ?? "N/A"}");
    }

    public void LogResume(string issueKey, string? summary, string? assignee, string? status)
    {
        var timestamp = DateTime.Now;
        LogToTextFile($"[{timestamp:yyyy-MM-dd HH:mm:ss}] RESUME - Issue: {issueKey}, Summary: {summary ?? "N/A"}, Assignee: {assignee ?? "N/A"}, Status: {status ?? "N/A"}");
    }

    public void LogCancelled(string issueKey, string? summary, string? assignee, string? status, TimeSpan discardedTime)
    {
        var timestamp = DateTime.Now;
        LogToTextFile($"[{timestamp:yyyy-MM-dd HH:mm:ss}] CANCELLED - Issue: {issueKey}, Summary: {summary ?? "N/A"}, Assignee: {assignee ?? "N/A"}, Status: {status ?? "N/A"}, Discarded: {FormatTimeSpan(discardedTime)}");
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

    /// <summary>
    /// Column layout matching user's file structure:
    /// A(1): Date, B(2): Ticket ID, C(3): Work Description
    /// D(4)/E(5): Mon decimal/h:mm, F(6)/G(7): Tue, H(8)/I(9): Wed
    /// J(10)/K(11): Thu, L(12)/M(13): Fri, N(14)/O(15): Sat, P(16)/Q(17): Sun
    /// </summary>
    private static class Columns
    {
        public const int Date = 1;
        public const int TicketId = 2;
        public const int WorkDescription = 3;

        // Day columns: (decimal, h:mm) pairs
        public static (int Decimal, int HMM) GetDayColumns(DayOfWeek day) => day switch
        {
            DayOfWeek.Monday => (4, 5),
            DayOfWeek.Tuesday => (6, 7),
            DayOfWeek.Wednesday => (8, 9),
            DayOfWeek.Thursday => (10, 11),
            DayOfWeek.Friday => (12, 13),
            DayOfWeek.Saturday => (14, 15),
            DayOfWeek.Sunday => (16, 17),
            _ => (4, 5)
        };

        // All decimal columns for SUM formulas
        public static readonly int[] DecimalColumns = { 4, 6, 8, 10, 12, 14, 16 };

        public const int LastColumn = 17;
    }

    private void LogToXlsx(string issueKey, string? workDescription, TimeSpan timeLogged, DateTime timestamp)
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
                var weekHeaderRow = FindWeekSection(worksheet, weekStart, weekEnd);
                if (weekHeaderRow == -1)
                {
                    weekHeaderRow = CreateWeekSection(worksheet, weekStart, weekEnd);
                }

                // Insert a new row at the bottom of the week section
                var newRow = InsertRowInWeekSection(worksheet, weekHeaderRow);

                // Write data to the new row
                worksheet.Cell(newRow, Columns.Date).Value = timestamp.Date;
                worksheet.Cell(newRow, Columns.TicketId).Value = issueKey;
                worksheet.Cell(newRow, Columns.WorkDescription).Value = workDescription ?? "";

                // Get column indices for the day
                var (decimalCol, hmmCol) = Columns.GetDayColumns(dayOfWeek);

                // Write decimal format rounded to 2 digits (e.g., 1.33)
                worksheet.Cell(newRow, decimalCol).Value = Math.Round(timeLogged.TotalHours, 2);

                // Write h:mm format (e.g., "2:16")
                worksheet.Cell(newRow, hmmCol).Value = FormatTimeSpanHMM(timeLogged);

                // Update SUM formulas in the week header to include the new row
                UpdateWeekSumFormulas(worksheet, weekHeaderRow);

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

    /// <summary>
    /// Inserts a new row at the bottom of the week section (before blank row or next week header).
    /// Returns the row number of the inserted row.
    /// </summary>
    private int InsertRowInWeekSection(IXLWorksheet worksheet, int weekHeaderRow)
    {
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? weekHeaderRow;

        // Find the end of this week's section
        int insertBeforeRow = lastRow + 1;
        for (int row = weekHeaderRow + 1; row <= lastRow; row++)
        {
            var cellValue = worksheet.Cell(row, Columns.Date).GetString();
            var isBold = worksheet.Cell(row, Columns.Date).Style.Font.Bold;

            // Found next week header or blank row before next section
            if (isBold && cellValue.Contains(" - "))
            {
                insertBeforeRow = row;
                break;
            }

            // Found blank separator row (check if next row is a week header)
            if (string.IsNullOrWhiteSpace(cellValue) && row + 1 <= lastRow)
            {
                var nextCellValue = worksheet.Cell(row + 1, Columns.Date).GetString();
                var nextIsBold = worksheet.Cell(row + 1, Columns.Date).Style.Font.Bold;
                if (nextIsBold && nextCellValue.Contains(" - "))
                {
                    insertBeforeRow = row;
                    break;
                }
            }
        }

        // Insert a new row at the insertion point
        var newRowNumber = insertBeforeRow;
        worksheet.Row(insertBeforeRow).InsertRowsAbove(1);

        return newRowNumber;
    }

    /// <summary>
    /// Updates the SUM formulas in the week header row to include all data rows in the section.
    /// </summary>
    private void UpdateWeekSumFormulas(IXLWorksheet worksheet, int weekHeaderRow)
    {
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? weekHeaderRow;

        // Find the range of data rows for this week
        int firstDataRow = weekHeaderRow + 1;
        int lastDataRow = weekHeaderRow;

        for (int row = weekHeaderRow + 1; row <= lastRow; row++)
        {
            var cellValue = worksheet.Cell(row, Columns.Date).GetString();
            var isBold = worksheet.Cell(row, Columns.Date).Style.Font.Bold;

            // Stop at next week header or blank row before next section
            if (isBold && cellValue.Contains(" - "))
                break;

            if (string.IsNullOrWhiteSpace(cellValue))
            {
                // Check if this is a separator before next week
                if (row + 1 <= lastRow)
                {
                    var nextCellValue = worksheet.Cell(row + 1, Columns.Date).GetString();
                    var nextIsBold = worksheet.Cell(row + 1, Columns.Date).Style.Font.Bold;
                    if (nextIsBold && nextCellValue.Contains(" - "))
                        break;
                }
            }

            lastDataRow = row;
        }

        // Update SUM formulas for each decimal column
        foreach (var col in Columns.DecimalColumns)
        {
            var colLetter = GetColumnLetter(col);
            var formula = $"SUM({colLetter}{firstDataRow}:{colLetter}{lastDataRow})";
            var cell = worksheet.Cell(weekHeaderRow, col);
            cell.FormulaA1 = formula;
            cell.Style.Font.Bold = true;
        }
    }

    private static string GetColumnLetter(int columnNumber)
    {
        string result = "";
        while (columnNumber > 0)
        {
            columnNumber--;
            result = (char)('A' + columnNumber % 26) + result;
            columnNumber /= 26;
        }
        return result;
    }

    private void InitializeWorksheet(IXLWorksheet worksheet)
    {
        // Header row matching user's structure
        worksheet.Cell(1, Columns.Date).Value = "Date";
        worksheet.Cell(1, Columns.TicketId).Value = "Ticket ID";
        worksheet.Cell(1, Columns.WorkDescription).Value = "Work Description";

        // Day columns: pairs of (decimal, h:mm)
        string[] days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        int col = 4;
        foreach (var day in days)
        {
            worksheet.Cell(1, col).Value = day;      // Decimal column
            worksheet.Cell(1, col + 1).Value = "";   // h:mm column (no header text)
            col += 2;
        }

        // Style header
        var headerRange = worksheet.Range(1, 1, 1, Columns.LastColumn);
        headerRange.Style.Font.Bold = true;
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

        // Section header with week range
        var sectionHeader = FormatWeekRange(weekStart, weekEnd);
        worksheet.Cell(sectionRow, Columns.Date).Value = sectionHeader;
        worksheet.Cell(sectionRow, Columns.Date).Style.Font.Bold = true;

        // Make columns B and C bold too (empty but styled like the header)
        worksheet.Cell(sectionRow, Columns.TicketId).Style.Font.Bold = true;
        worksheet.Cell(sectionRow, Columns.WorkDescription).Style.Font.Bold = true;

        // Add placeholder SUM formulas for each decimal column
        // They will be updated when the first row is added
        var firstDataRow = sectionRow + 1;
        foreach (var col in Columns.DecimalColumns)
        {
            var colLetter = GetColumnLetter(col);
            var formula = $"SUM({colLetter}{firstDataRow}:{colLetter}{firstDataRow})";
            var cell = worksheet.Cell(sectionRow, col);
            cell.FormulaA1 = formula;
            cell.Style.Font.Bold = true;
        }

        return sectionRow;
    }

    private string FormatWeekRange(DateTime weekStart, DateTime weekEnd)
    {
        return $"{weekStart:MMM dd} - {weekEnd:MMM dd}";
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
    /// Formats time as h:mm (e.g., "2:16", "0:30")
    /// </summary>
    private string FormatTimeSpanHMM(TimeSpan time)
    {
        var hours = (int)time.TotalHours;
        var minutes = time.Minutes;
        return $"{hours}:{minutes:D2}";
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
                        // Column 1 might be date or ticket ID depending on format
                        // In new format: A=Date, B=TicketID, C=Description
                        // In old format: A=TicketID, B=Summary
                        var colBValue = worksheet.Cell(row, 2).GetString();
                        var actualTicketId = ticketId;
                        var summary = colBValue;

                        // Check if this is new format (ticketId looks like a date)
                        if (DateTime.TryParse(ticketId, out _))
                        {
                            // New format: A=Date, B=TicketID, C=Description
                            actualTicketId = colBValue;
                            summary = worksheet.Cell(row, 3).GetString();
                        }

                        var entry = new TimeLogEntry
                        {
                            TicketId = actualTicketId,
                            Summary = summary
                        };

                        // Try reading from new decimal columns (D=4, F=6, H=8, J=10, L=12, N=14, P=16)
                        bool foundData = false;
                        DayOfWeek[] days = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                                            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

                        foreach (var day in days)
                        {
                            var (decimalCol, _) = Columns.GetDayColumns(day);
                            var hours = worksheet.Cell(row, decimalCol).Value.IsNumber
                                ? worksheet.Cell(row, decimalCol).Value.GetNumber()
                                : 0.0;

                            if (hours > 0)
                            {
                                entry.DailyHours[day] = hours;
                                foundData = true;
                            }
                        }

                        // Fallback: try old format columns 3-9
                        if (!foundData)
                        {
                            for (int col = 3; col <= 9; col++)
                            {
                                var hours = worksheet.Cell(row, col).Value.IsNumber
                                    ? worksheet.Cell(row, col).Value.GetNumber()
                                    : 0.0;

                                if (hours > 0)
                                {
                                    var day = (DayOfWeek)(col - 2); // 3->Mon(1), 4->Tue(2), etc.
                                    if (col == 3) day = DayOfWeek.Monday;
                                    else if (col == 4) day = DayOfWeek.Tuesday;
                                    else if (col == 5) day = DayOfWeek.Wednesday;
                                    else if (col == 6) day = DayOfWeek.Thursday;
                                    else if (col == 7) day = DayOfWeek.Friday;
                                    else if (col == 8) day = DayOfWeek.Saturday;
                                    else if (col == 9) day = DayOfWeek.Sunday;
                                    entry.DailyHours[day] = hours;
                                }
                            }
                        }

                        if (entry.TotalHours > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[TimeLogService] Found entry: {actualTicketId} with {entry.TotalHours}h total");
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

    private DateTime GetDateForDay(DateTime weekStart, DayOfWeek targetDay)
    {
        var daysToAdd = ((int)targetDay - (int)weekStart.DayOfWeek + 7) % 7;
        return weekStart.AddDays(daysToAdd);
    }
}
