using Microsoft.Toolkit.Uwp.Notifications;

namespace UpworkJiraTracker.Service;

/// <summary>
/// Service for showing Windows toast notifications
/// </summary>
public static class NotificationService
{
    /// <summary>
    /// Shows a success notification with green checkmark
    /// </summary>
    public static void ShowSuccess(string title, string? message = null, int durationMs = 3000)
    {
        var builder = new ToastContentBuilder()
            .AddText(title);

        if (!string.IsNullOrWhiteSpace(message))
        {
            builder.AddText(message);
        }

        builder.Show(toast =>
        {
            toast.ExpirationTime = DateTime.Now.AddMilliseconds(durationMs);
        });
    }

    /// <summary>
    /// Shows an error notification
    /// </summary>
    public static void ShowError(string title, string? message = null, int durationMs = 5000)
    {
        var builder = new ToastContentBuilder()
            .AddText(title);

        if (!string.IsNullOrWhiteSpace(message))
        {
            builder.AddText(message);
        }

        builder.Show(toast =>
        {
            toast.ExpirationTime = DateTime.Now.AddMilliseconds(durationMs);
        });
    }
}
