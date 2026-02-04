using System.Diagnostics;

namespace UpworkJiraTracker.Service;

/// <summary>
/// Service for automating Deel time tracking operations.
/// Uses embedded WebView2 browser with API-based logging.
/// </summary>
public class DeelAutomationService
{
    /// <summary>
    /// Test Deel connection by opening browser and checking authentication
    /// </summary>
    /// <returns>Tuple of (success, errorMessage)</returns>
    public async Task<(bool Success, string? ErrorMessage)> TestConnectionAsync()
    {
        try
        {
            Debug.WriteLine("[DeelAutomationService] Starting connection test...");

            var service = DeelEmbeddedBrowserService.Instance;
            var authenticated = await service.OpenAndWaitForAuthenticationAsync();

            if (!authenticated)
            {
                return (false, "Failed to authenticate");
            }

            Debug.WriteLine("[DeelAutomationService] Authenticated successfully");

            // Show toast notification
            NotificationService.ShowSuccess(
                "Deel Connected",
                "Authentication successful",
                durationMs: 3000
            );

            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeelAutomationService] Test failed: {ex.Message}\n{ex.StackTrace}");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Log hours through embedded browser with API calls.
    /// Opens hidden browser, checks auth, shows window if login needed.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> LogHoursAsync(
        int hours,
        int minutes,
        string description)
    {
        try
        {
            Debug.WriteLine($"[DeelAutomationService] Logging hours via embedded browser: {hours}h {minutes}m - {description}");

            var service = DeelEmbeddedBrowserService.Instance;
            var (success, error) = await service.LogHoursSilentlyAsync(hours, minutes, description);

            if (success)
            {
                Debug.WriteLine("[DeelAutomationService] Hours logged successfully via API");
                return (true, null);
            }

            Debug.WriteLine($"[DeelAutomationService] Failed to log hours: {error}");
            return (false, error);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeelAutomationService] Log hours failed: {ex.Message}");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Close the Deel browser window
    /// </summary>
    public async Task CloseAsync()
    {
        try
        {
            var service = DeelEmbeddedBrowserService.Instance;
            service.CloseBrowserWindow();
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeelAutomationService] Close failed: {ex.Message}");
        }
    }
}
