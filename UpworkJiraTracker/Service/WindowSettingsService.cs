using System.Text.Json;
using System.Windows;

using UpworkJiraTracker.Model;

namespace UpworkJiraTracker.Service;

public class WindowSettingsService
{
    public WindowSettings Load()
    {
        var result = new WindowSettings();

        try
        {
            var settings = Properties.Settings.Default;

            result.WindowLeft = settings.WindowLeft;
            result.WindowTop = settings.WindowTop;
            result.WindowWidth = settings.MainWindowWidth > 0 ? settings.MainWindowWidth : 180;
            result.WindowHeight = settings.MainWindowHeight > 0 ? settings.MainWindowHeight : 48;
            result.CustomBackgroundColor = settings.CustomBackgroundColor;
            result.LogDirectory = string.IsNullOrEmpty(settings.LogDirectory) ? "." : settings.LogDirectory;

            if (!string.IsNullOrEmpty(settings.TimezonesJson))
            {
                var loadedTimezones = JsonSerializer.Deserialize<List<TimezoneEntry>>(settings.TimezonesJson);
                if (loadedTimezones != null && loadedTimezones.Count > 0)
                {
                    result.Timezones = loadedTimezones;
                }
            }

            // Set defaults if no position saved
            if (result.WindowLeft == 0 && result.WindowTop == 0)
            {
                result.WindowLeft = 0;
                result.WindowTop = SystemParameters.WorkArea.Bottom - result.WindowHeight;
            }

            // Default timezones if none saved
            if (result.Timezones.Count == 0)
            {
                result.Timezones.Add(new TimezoneEntry { Caption = "PST", TimeZoneId = Constants.Timezones.DefaultTimezone1 });
                result.Timezones.Add(new TimezoneEntry { Caption = "GMT", TimeZoneId = Constants.Timezones.DefaultTimezone2 });
            }
        }
        catch
        {
            // Return defaults on error
            result.WindowLeft = 0;
            result.WindowTop = SystemParameters.WorkArea.Bottom - 48;
            result.WindowWidth = 180;
            result.WindowHeight = 48;
            result.Timezones.Add(new TimezoneEntry { Caption = "PST", TimeZoneId = Constants.Timezones.DefaultTimezone1 });
            result.Timezones.Add(new TimezoneEntry { Caption = "GMT", TimeZoneId = Constants.Timezones.DefaultTimezone2 });
        }

        return result;
    }

    public void Save(WindowSettings windowSettings)
    {
        try
        {
            var settings = Properties.Settings.Default;

            settings.WindowLeft = windowSettings.WindowLeft;
            settings.WindowTop = windowSettings.WindowTop;
            settings.MainWindowWidth = windowSettings.WindowWidth;
            settings.MainWindowHeight = windowSettings.WindowHeight;
            settings.CustomBackgroundColor = windowSettings.CustomBackgroundColor ?? string.Empty;
            settings.LogDirectory = windowSettings.LogDirectory ?? ".";

            if (windowSettings.Timezones.Count > 0)
            {
                settings.TimezonesJson = JsonSerializer.Serialize(windowSettings.Timezones);
            }

            settings.Save();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    public void SavePosition(double left, double top)
    {
        try
        {
            var settings = Properties.Settings.Default;
            settings.WindowLeft = left;
            settings.WindowTop = top;
            settings.Save();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save position: {ex.Message}");
        }
    }
}
