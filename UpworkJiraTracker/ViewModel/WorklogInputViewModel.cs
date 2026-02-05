using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using UpworkJiraTracker.Helper;

namespace UpworkJiraTracker.ViewModel;

public class WorklogInputViewModel : INotifyPropertyChanged
{
    private string _workDescription = string.Empty;
    private string _etaText = string.Empty;

    // Regex patterns for Jira time format: "2d", "3h", "30m", "2d 3h", "1h 30m", etc.
    private static readonly Regex DaysPattern = new(@"(\d+(?:\.\d+)?)\s*d", RegexOptions.IgnoreCase);
    private static readonly Regex HoursPattern = new(@"(\d+(?:\.\d+)?)\s*h", RegexOptions.IgnoreCase);
    private static readonly Regex MinutesPattern = new(@"(\d+(?:\.\d+)?)\s*m", RegexOptions.IgnoreCase);

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? SubmitRequested;
    public event EventHandler? CancelRequested;
    public event EventHandler? DiscardRequested;

    public ICommand SubmitCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand DiscardCommand { get; }

    public string WorkDescription
    {
        get => _workDescription;
        set
        {
            if (_workDescription != value)
            {
                _workDescription = value;
                OnPropertyChanged();
            }
        }
    }

    public string EtaText
    {
        get => _etaText;
        set
        {
            if (_etaText != value)
            {
                _etaText = value;
                OnPropertyChanged();
            }
        }
    }

    public double? RemainingEstimateHours { get; private set; }

    /// <summary>
    /// The raw ETA text to pass directly to Jira (in Jira time format like "2h", "1h 30m")
    /// </summary>
    public string? RemainingEstimateJiraFormat { get; private set; }

    public WorklogInputViewModel()
    {
        SubmitCommand = new RelayCommand(_ => Submit());
        CancelCommand = new RelayCommand(_ => Cancel());
        DiscardCommand = new RelayCommand(_ => Discard());
    }

    private void Submit()
    {
        // Parse ETA
        var etaText = EtaText?.Trim();
        System.Diagnostics.Debug.WriteLine($"[WorklogInputViewModel] Submit called. EtaText='{etaText}'");

        if (!string.IsNullOrEmpty(etaText))
        {
            // First try parsing as Jira time format (e.g., "2h", "1h 30m", "2d")
            var jiraHours = ParseJiraTimeFormat(etaText);
            if (jiraHours.HasValue)
            {
                RemainingEstimateHours = jiraHours.Value;
                RemainingEstimateJiraFormat = etaText; // Keep original format for Jira
                System.Diagnostics.Debug.WriteLine($"[WorklogInputViewModel] Parsed Jira format: RemainingEstimateHours={jiraHours.Value}, JiraFormat='{etaText}'");
            }
            else
            {
                // Fall back to decimal hours (e.g., "2.5" or "2,5")
                var normalizedText = etaText.Replace(',', '.');
                if (double.TryParse(normalizedText, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var hours) && hours >= 0)
                {
                    RemainingEstimateHours = hours;
                    RemainingEstimateJiraFormat = null; // Will be converted to Jira format later
                    System.Diagnostics.Debug.WriteLine($"[WorklogInputViewModel] Parsed decimal hours: RemainingEstimateHours={hours}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[WorklogInputViewModel] Failed to parse EtaText='{etaText}'");
                }
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[WorklogInputViewModel] EtaText is empty, RemainingEstimateHours will be null");
        }

        SubmitRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Parses Jira time format strings like "2d", "3h", "30m", "2d 3h", "1h 30m"
    /// and returns the total hours as a double.
    /// </summary>
    private static double? ParseJiraTimeFormat(string text)
    {
        double totalHours = 0;
        bool foundAnyMatch = false;

        // Look for days (assuming 8 hours per day, which is Jira default)
        var daysMatch = DaysPattern.Match(text);
        if (daysMatch.Success)
        {
            if (double.TryParse(daysMatch.Groups[1].Value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var days))
            {
                totalHours += days * 8; // 8 hours per day
                foundAnyMatch = true;
            }
        }

        // Look for hours
        var hoursMatch = HoursPattern.Match(text);
        if (hoursMatch.Success)
        {
            if (double.TryParse(hoursMatch.Groups[1].Value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var hours))
            {
                totalHours += hours;
                foundAnyMatch = true;
            }
        }

        // Look for minutes
        var minutesMatch = MinutesPattern.Match(text);
        if (minutesMatch.Success)
        {
            if (double.TryParse(minutesMatch.Groups[1].Value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var minutes))
            {
                totalHours += minutes / 60.0;
                foundAnyMatch = true;
            }
        }

        if (foundAnyMatch && totalHours >= 0)
        {
            return totalHours;
        }

        return null;
    }

    private void Cancel()
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Discard()
    {
        DiscardRequested?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
