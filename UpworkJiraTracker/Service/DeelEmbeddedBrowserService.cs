using System.Diagnostics;
using System.Windows;
using UpworkJiraTracker.View;

namespace UpworkJiraTracker.Service;

/// <summary>
/// Alternative Deel integration using embedded WebView2 browser in a WPF window
/// </summary>
public class DeelEmbeddedBrowserService
{
    private static DeelEmbeddedBrowserService? _instance;
    private static readonly object _lock = new();

    private DeelBrowserWindow? _browserWindow;
    private bool _isAuthenticated = false;

    /// <summary>
    /// Singleton instance
    /// </summary>
    public static DeelEmbeddedBrowserService Instance
    {
        get
        {
            lock (_lock)
            {
                _instance ??= new DeelEmbeddedBrowserService();
                return _instance;
            }
        }
    }

    /// <summary>
    /// Whether the user is authenticated
    /// </summary>
    public bool IsAuthenticated => _isAuthenticated;

    /// <summary>
    /// Whether the browser window exists (may be hidden)
    /// </summary>
    public bool IsWindowOpen => _browserWindow != null;

    /// <summary>
    /// Whether the browser window is visible
    /// </summary>
    public bool IsWindowVisible => _browserWindow != null && _browserWindow.IsVisible && !_browserWindow.IsHiddenState;

    /// <summary>
    /// Open the Deel browser window for authentication (visible)
    /// </summary>
    public void OpenBrowserWindow()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            EnsureWindowExists();
            _browserWindow!.ShowWindow();
        });
    }

    /// <summary>
    /// Ensure window exists, creating it hidden if needed
    /// </summary>
    private void EnsureWindowExists()
    {
        if (_browserWindow == null)
        {
            _browserWindow = new DeelBrowserWindow();
            _browserWindow.AuthenticationChanged += OnAuthenticationChanged;
            _browserWindow.Closed += OnWindowClosed;
        }
    }

    /// <summary>
    /// Log hours silently - opens hidden browser, checks auth, shows window if login needed
    /// </summary>
    /// <param name="hours">Hours to log</param>
    /// <param name="minutes">Minutes to log</param>
    /// <param name="description">Work description</param>
    /// <returns>Success and optional error message</returns>
    public async Task<(bool Success, string? ErrorMessage)> LogHoursSilentlyAsync(int hours, int minutes, string description)
    {
        try
        {
            Debug.WriteLine($"[DeelEmbeddedBrowserService] LogHoursSilentlyAsync: {hours}h {minutes}m");

            bool wasHidden = false;
            bool windowExisted = false;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                windowExisted = _browserWindow != null;
                if (windowExisted)
                {
                    wasHidden = _browserWindow!.IsHiddenState;
                }

                // Create or reuse window, start hidden if new
                EnsureWindowExists();

                if (!windowExisted)
                {
                    // New window - start hidden
                    _browserWindow!.StartHidden();
                    _browserWindow.Show();
                    Debug.WriteLine("[DeelEmbeddedBrowserService] Created new hidden browser window");
                }
            });

            // Wait for page to be ready and check authentication
            var isAuthenticated = await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                return await _browserWindow!.WaitForAuthenticationCheckAsync(maxRetries: 15, delayMs: 500);
            }).Result;

            if (!isAuthenticated)
            {
                Debug.WriteLine("[DeelEmbeddedBrowserService] Not authenticated - showing window for login");

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _browserWindow!.ShowWindow();
                });

                // Wait for user to authenticate (with timeout)
                var authTask = Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    return await _browserWindow!.WaitForAuthenticationAsync();
                }).Result;

                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
                var completedTask = await Task.WhenAny(authTask, timeoutTask);

                if (completedTask == timeoutTask || !await authTask)
                {
                    Debug.WriteLine("[DeelEmbeddedBrowserService] Authentication timeout or failed");
                    return (false, "Authentication required - please log in to Deel");
                }
            }

            // Now authenticated - log hours via API
            Debug.WriteLine("[DeelEmbeddedBrowserService] Authenticated - logging hours via API");

            var (success, error) = await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                return await _browserWindow!.LogHoursViaApiAsync(hours, minutes, description);
            }).Result;

            if (success)
            {
                Debug.WriteLine("[DeelEmbeddedBrowserService] Hours logged successfully");

                // Keep window state unchanged (if was hidden, stay hidden; if visible, stay visible)
                // The window state is preserved automatically

                return (true, null);
            }

            Debug.WriteLine($"[DeelEmbeddedBrowserService] Failed to log hours: {error}");
            return (false, error);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeelEmbeddedBrowserService] Error: {ex.Message}");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Open browser and wait for authentication
    /// </summary>
    public async Task<bool> OpenAndWaitForAuthenticationAsync()
    {
        var tcs = new TaskCompletionSource<bool>();

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            EnsureWindowExists();

            if (!_browserWindow!.IsVisible || _browserWindow.IsHiddenState)
            {
                _browserWindow.AuthenticationChanged += (s, authenticated) =>
                {
                    if (authenticated)
                        tcs.TrySetResult(true);
                };
                _browserWindow.ShowWindow();
            }
            else
            {
                _browserWindow.Activate();
                if (_browserWindow.IsAuthenticated)
                    tcs.TrySetResult(true);
            }
        });

        // Wait for auth or window close with timeout
        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            Debug.WriteLine("[DeelEmbeddedBrowserService] Authentication timeout");
            return false;
        }

        return await tcs.Task;
    }

    /// <summary>
    /// Log hours using the embedded browser (with visible window)
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> LogHoursAsync(int hours, int minutes, string description)
    {
        try
        {
            Debug.WriteLine($"[DeelEmbeddedBrowserService] LogHoursAsync: {hours}h {minutes}m");

            // Open visible window if not open
            if (_browserWindow == null || !_browserWindow.IsVisible || _browserWindow.IsHiddenState)
            {
                var authenticated = await OpenAndWaitForAuthenticationAsync();
                if (!authenticated)
                {
                    return (false, "Not authenticated");
                }
            }

            // Wait a bit for window to be ready
            await Task.Delay(500);

            bool success = false;
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                if (_browserWindow != null && _browserWindow.IsAuthenticated)
                {
                    var (ok, _) = await _browserWindow.LogHoursViaApiAsync(hours, minutes, description);
                    success = ok;
                }
            });

            if (success)
            {
                return (true, null);
            }

            return (false, "Failed to log hours");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeelEmbeddedBrowserService] Error: {ex.Message}");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Hide the browser window (keep it running in background)
    /// </summary>
    public void HideBrowserWindow()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _browserWindow?.HideWindow();
        });
    }

    /// <summary>
    /// Close the browser window completely
    /// </summary>
    public void CloseBrowserWindow()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _browserWindow?.Close();
            _browserWindow = null;
        });
    }

    private void OnAuthenticationChanged(object? sender, bool authenticated)
    {
        _isAuthenticated = authenticated;
        Debug.WriteLine($"[DeelEmbeddedBrowserService] Authentication changed: {authenticated}");
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (_browserWindow != null)
        {
            _browserWindow.AuthenticationChanged -= OnAuthenticationChanged;
            _browserWindow.Closed -= OnWindowClosed;
        }
        _browserWindow = null;
        Debug.WriteLine("[DeelEmbeddedBrowserService] Browser window closed");
    }
}
