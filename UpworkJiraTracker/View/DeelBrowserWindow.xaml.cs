using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using UpworkJiraTracker.Service;
using UpworkJiraTracker.ViewModel;

namespace UpworkJiraTracker.View;

/// <summary>
/// Deel browser window with embedded WebView2 for authentication and API-based time tracking
/// </summary>
public partial class DeelBrowserWindow : Window
{
    private readonly DeelBrowserViewModel _viewModel;
    private bool _isInitialized;
    private bool _isNavigationComplete;
    private bool _isHidden;
    private TaskCompletionSource<bool>? _authenticationTcs;
    private TaskCompletionSource<bool>? _readyTcs;
    private DeelApiClient? _apiClient;

    public event EventHandler<bool>? AuthenticationChanged;

    public bool IsAuthenticated => _viewModel.IsAuthenticated;
    public bool IsHiddenState => _isHidden;

    public DeelBrowserWindow()
    {
        InitializeComponent();
        _viewModel = new DeelBrowserViewModel();
        DataContext = _viewModel;
        InitializeWebView();
    }

    public void StartHidden()
    {
        _isHidden = true;
        ShowInTaskbar = false;
        WindowState = WindowState.Minimized;
        Visibility = Visibility.Hidden;
    }

    public void ShowWindow()
    {
        _isHidden = false;
        ShowInTaskbar = true;
        Visibility = Visibility.Visible;
        WindowState = WindowState.Normal;
        Activate();
    }

    public void HideWindow()
    {
        _isHidden = true;
        ShowInTaskbar = false;
        WindowState = WindowState.Minimized;
        Visibility = Visibility.Hidden;
    }

    public Task<bool> WaitForAuthenticationAsync()
    {
        if (_viewModel.IsAuthenticated)
            return Task.FromResult(true);

        _authenticationTcs = new TaskCompletionSource<bool>();
        return _authenticationTcs.Task;
    }

    public Task<bool> WaitForReadyAsync()
    {
        if (_isInitialized && _isNavigationComplete)
            return Task.FromResult(true);

        _readyTcs = new TaskCompletionSource<bool>();
        return _readyTcs.Task;
    }

    public async Task<bool> CheckAuthenticationViaCdpAsync()
    {
        if (WebView.CoreWebView2 == null)
            return false;

        try
        {
            var docResult = await WebView.CoreWebView2.CallDevToolsProtocolMethodAsync("DOM.getDocument", "{}");
            var docJson = System.Text.Json.JsonDocument.Parse(docResult);
            var rootNodeId = docJson.RootElement.GetProperty("root").GetProperty("nodeId").GetInt32();

            var profileSelector = Constants.Deel.Selectors.ProfileElement;
            var parameters = System.Text.Json.JsonSerializer.Serialize(new { nodeId = rootNodeId, selector = profileSelector });
            var result = await WebView.CoreWebView2.CallDevToolsProtocolMethodAsync("DOM.querySelector", parameters);
            var json = System.Text.Json.JsonDocument.Parse(result);
            var nodeId = json.RootElement.GetProperty("nodeId").GetInt32();

            var found = nodeId > 0;
            Debug.WriteLine($"[DeelBrowserWindow] CDP auth check - Profile found: {found}");
            return found;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeelBrowserWindow] CDP auth check error: {ex.Message}");
            return false;
        }
    }

    public bool IsOnLoginPage()
    {
        if (WebView.CoreWebView2 == null)
            return false;

        var url = WebView.CoreWebView2.Source;
        return url.Contains("/login") || url.Contains("/signin");
    }

    public async Task<bool> WaitForAuthenticationCheckAsync(int maxRetries = 10, int delayMs = 500)
    {
        Debug.WriteLine("[DeelBrowserWindow] Starting authentication check loop...");

        if (!_isInitialized || !_isNavigationComplete)
        {
            await WaitForReadyAsync();
        }

        for (int i = 0; i < maxRetries; i++)
        {
            Debug.WriteLine($"[DeelBrowserWindow] Auth check attempt {i + 1}/{maxRetries}");

            var isAuthenticated = await CheckAuthenticationViaCdpAsync();
            if (isAuthenticated)
            {
                Debug.WriteLine("[DeelBrowserWindow] Authenticated via CDP check");
                _viewModel.IsAuthenticated = true;
                AuthenticationChanged?.Invoke(this, true);
                return true;
            }

            if (IsOnLoginPage())
            {
                Debug.WriteLine("[DeelBrowserWindow] On login page - login required");
                return false;
            }

            await Task.Delay(delayMs);
        }

        Debug.WriteLine("[DeelBrowserWindow] Auth check timeout - assuming login required");
        return false;
    }

    public async Task<DeelApiClient?> ExtractCookiesAndCreateClientAsync()
    {
        if (WebView.CoreWebView2 == null)
            return null;

        try
        {
            var cookieManager = WebView.CoreWebView2.CookieManager;
            var webViewCookies = await cookieManager.GetCookiesAsync(Constants.Deel.BaseUrl);

            Debug.WriteLine($"[DeelBrowserWindow] Extracted {webViewCookies.Count} cookies");

            var client = new DeelApiClient();
            var cookies = new List<Cookie>();

            foreach (var wvCookie in webViewCookies)
            {
                var cookie = new Cookie(wvCookie.Name, wvCookie.Value, wvCookie.Path, wvCookie.Domain)
                {
                    Secure = wvCookie.IsSecure,
                    HttpOnly = wvCookie.IsHttpOnly
                };
                cookies.Add(cookie);
            }

            client.SetCookies(cookies);
            client.SetPageUrl(WebView.CoreWebView2.Source);

            return client;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeelBrowserWindow] Error extracting cookies: {ex.Message}");
            return null;
        }
    }

    public async Task<(bool Success, string? Error)> LogHoursViaApiAsync(int hours, int minutes, string description)
    {
        _apiClient = await ExtractCookiesAndCreateClientAsync();

        if (_apiClient == null)
        {
            return (false, "No API client available");
        }

        try
        {
            Dispatcher.Invoke(() => _viewModel.StatusText = " - Logging hours...");

            var contractsResponse = await _apiClient.GetContractsAsync();
            if (contractsResponse?.Success != true || contractsResponse.Data?.Contracts == null || contractsResponse.Data.Contracts.Count == 0)
            {
                return (false, contractsResponse?.Error ?? "No contracts found");
            }

            var contract = contractsResponse.Data.Contracts.First();
            Debug.WriteLine($"[DeelBrowserWindow] Found contract: {contract.ContractId}");

            double totalWorkedHours = hours + (minutes / 60.0);
            var shiftResponse = await _apiClient.LogShiftAsync(contract.ContractId, totalWorkedHours, description);
            if (shiftResponse?.Success != true || shiftResponse.Data?.Shift == null)
            {
                return (false, shiftResponse?.Error ?? "Failed to log shift");
            }

            var shift = shiftResponse.Data.Shift;
            Debug.WriteLine($"[DeelBrowserWindow] Shift logged: {shift.TotalWorkedHours}h, Payment: {shift.CalculatedPayment}");

            Dispatcher.Invoke(() => _viewModel.StatusText = " - Hours logged!");

            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeelBrowserWindow] API error: {ex.Message}");
            Dispatcher.Invoke(() => _viewModel.StatusText = $" - Error: {ex.Message}");
            return (false, ex.Message);
        }
    }

    private async void InitializeWebView()
    {
        try
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UpworkJiraTracker",
                "DeelWebView2"
            );
            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await WebView.EnsureCoreWebView2Async(env);

            _isInitialized = true;
            Debug.WriteLine("[DeelBrowserWindow] WebView2 initialized");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeelBrowserWindow] WebView2 initialization failed: {ex.Message}");
            _viewModel.StatusText = $" - Error: {ex.Message}";
        }
    }

    private void WebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            Debug.WriteLine("[DeelBrowserWindow] CoreWebView2 ready, navigating to Deel...");
            var url = new Uri(new Uri(Constants.Deel.BaseUrl), Constants.Deel.ContractsUrl);
            WebView.CoreWebView2.Navigate(url.AbsoluteUri);
        }
        else
        {
            Debug.WriteLine($"[DeelBrowserWindow] CoreWebView2 init failed: {e.InitializationException?.Message}");
            _viewModel.StatusText = " - Browser init failed";
        }
    }

    private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            _viewModel.StatusText = " - Navigation failed";
            _isNavigationComplete = true;
            _readyTcs?.TrySetResult(false);
            return;
        }

        var url = WebView.CoreWebView2.Source;
        Debug.WriteLine($"[DeelBrowserWindow] Navigation completed: {url}");

        _isNavigationComplete = true;

        var wasAuthenticated = _viewModel.IsAuthenticated;
        var urlBasedAuth = url.StartsWith(Constants.Deel.BaseUrl) && url.Contains(Constants.Deel.ContractsUrl);

        if (urlBasedAuth)
        {
            var cdpAuth = await CheckAuthenticationViaCdpAsync();
            _viewModel.IsAuthenticated = cdpAuth;

            if (_viewModel.IsAuthenticated)
            {
                _viewModel.StatusText = " - Connected";
                _apiClient = await ExtractCookiesAndCreateClientAsync();

                if (!wasAuthenticated)
                {
                    AuthenticationChanged?.Invoke(this, true);
                    _authenticationTcs?.TrySetResult(true);
                }
            }
            else
            {
                _viewModel.StatusText = " - Please log in";
            }
        }
        else
        {
            _viewModel.StatusText = " - Please log in";
        }

        _readyTcs?.TrySetResult(true);
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _authenticationTcs?.TrySetResult(false);
        _readyTcs?.TrySetResult(false);
        _apiClient?.Dispose();
        _apiClient = null;
    }
}
