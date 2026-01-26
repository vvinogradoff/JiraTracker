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

    /// <summary>
    /// Shows a warning notification
    /// </summary>
    public static void ShowWarning(string title, string? message = null, int durationMs = 4000)
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
    /// Shows an info notification
    /// </summary>
    public static void ShowInfo(string title, string? message = null, int durationMs = 3000)
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
    /// Shows a notification for time logged to a Jira issue
    /// </summary>
    public static void ShowTimeLogged(string issueKey, string timeFormatted, string? message = null)
    {
        var builder = new ToastContentBuilder()
            .AddArgument("action", "viewIssue")
            .AddArgument("issueKey", issueKey)
            .AddText($"{issueKey} - Logged {timeFormatted}");

        if (!string.IsNullOrWhiteSpace(message))
        {
            builder.AddText(message);
        }

        builder.Show(toast =>
        {
            toast.ExpirationTime = DateTime.Now.AddSeconds(5);
        });
    }

    /// <summary>
    /// Clears all notifications from this app
    /// </summary>
    public static void ClearAllNotifications()
    {
        ToastNotificationManagerCompat.History.Clear();
    }
}
