using Microsoft.Toolkit.Uwp.Notifications;
using WpfApplication = System.Windows.Application;

namespace UpworkJiraTracker;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : WpfApplication
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        // Subscribe to notification activation before showing any UI
        // This handles when user clicks on a notification
        ToastNotificationManagerCompat.OnActivated += toastArgs =>
        {
            // Parse the arguments from the notification
            ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

            // Need to dispatch to UI thread for any UI operations
            Current.Dispatcher.Invoke(() =>
            {
                // Handle different actions based on arguments
                if (args.TryGetValue("action", out string? action))
                {
                    switch (action)
                    {
                        case "viewIssue":
                            args.TryGetValue("issueKey", out string? issueKey);
                            // Could navigate to issue or copy to clipboard
                            break;

                        case "dismiss":
                            // User dismissed the notification
                            break;
                    }
                }

                // Bring window to foreground if needed
                if (MainWindow != null)
                {
                    MainWindow.Activate();
                }
            });
        };

        // Check if app was launched from a toast notification
        if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
        {
            // App was launched by clicking a notification
            // The OnActivated handler above will be called
        }
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        // Clean up toast notifications on exit for portable app
        // For installed apps, call this in your uninstaller instead
        // ToastNotificationManagerCompat.Uninstall();

        base.OnExit(e);
    }
}
