using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Web;

namespace UpworkJiraTracker.Service;

public class JiraOAuthService : IDisposable
{
    private HttpListener? _listener;
    private string? _accessToken;
    private string? _refreshToken;
    private string? _cloudId;
    private DateTime _tokenExpiresAtUtc;
    private string? _clientId;
    private string? _clientSecret;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);
    public bool IsTokenExpired => DateTime.UtcNow >= _tokenExpiresAtUtc;
    public bool HasRefreshToken => !string.IsNullOrEmpty(_refreshToken);
    public string? CloudId => _cloudId;
    public string? AccessToken => _accessToken;
    public bool HasCredentials => !string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(_clientSecret);

    public event EventHandler<string>? AuthenticationCompleted;
    public event EventHandler<string>? AuthenticationFailed;
    public event EventHandler? Disconnected;

    public JiraOAuthService()
    {
        LoadStoredCredentialsAndTokens();
    }

    public void SetCredentials(string clientId, string clientSecret)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        SaveCredentials();
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_refreshToken) && !string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(_clientSecret))
            {
                using var httpClient = new HttpClient();
                var revokeRequest = new Dictionary<string, string>
                {
                    { "token", _refreshToken },
                    { "token_type_hint", "refresh_token" },
                    { "client_id", _clientId },
                    { "client_secret", _clientSecret }
                };

                await httpClient.PostAsync("https://auth.atlassian.com/oauth/token/revoke",
                    new FormUrlEncodedContent(revokeRequest));
            }
        }
        catch
        {
            // Ignore revocation errors
        }
        finally
        {
            _accessToken = null;
            _refreshToken = null;
            _cloudId = null;
            _tokenExpiresAtUtc = DateTime.MinValue;

            var settings = Properties.Settings.Default;
            settings.JiraAccessToken = "";
            settings.JiraRefreshToken = "";
            settings.JiraCloudId = "";
            settings.JiraTokenExpiry = "";
            settings.Save();

            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task<bool> StartAuthenticationFlowAsync()
    {
        try
        {
            if (!HasCredentials)
            {
                return false;
            }

            _listener = new HttpListener();
            _listener.Prefixes.Add(Constants.Jira.CallbackUrl + "/");
            _listener.Start();

            var state = Guid.NewGuid().ToString();
            var authUrl = $"{Constants.Jira.AuthorizationUrl}?" +
                $"audience=api.atlassian.com&" +
                $"client_id={_clientId}&" +
                $"scope={Uri.EscapeDataString(Constants.Jira.Scopes)}&" +
                $"redirect_uri={Uri.EscapeDataString(Constants.Jira.CallbackUrl)}&" +
                $"state={state}&" +
                $"response_type=code&" +
                $"prompt=consent";

            OpenBrowser(authUrl);

            var context = await _listener.GetContextAsync();
            var request = context.Request;
            var response = context.Response;

            var queryParams = HttpUtility.ParseQueryString(request.Url?.Query ?? "");
            var code = queryParams["code"];
            var returnedState = queryParams["state"];
            var error = queryParams["error"];

            if (!string.IsNullOrEmpty(error))
            {
                SendResponse(response, "Authorization failed. You can close this window.");
                AuthenticationFailed?.Invoke(this, error);
                return false;
            }

            if (string.IsNullOrEmpty(code) || returnedState != state)
            {
                SendResponse(response, "Authorization failed. You can close this window.");
                AuthenticationFailed?.Invoke(this, "Invalid authorization response");
                return false;
            }

            SendResponse(response, "Authorization successful! You can close this window.");

            var success = await ExchangeCodeForTokenAsync(code);
            if (success)
            {
                await FetchCloudIdAsync();
                AuthenticationCompleted?.Invoke(this, "Authentication successful");
            }

            return success;
        }
        catch (Exception ex)
        {
            AuthenticationFailed?.Invoke(this, ex.Message);
            return false;
        }
        finally
        {
            _listener?.Stop();
            _listener = null;
        }
    }

    private async Task<bool> ExchangeCodeForTokenAsync(string code)
    {
        using var httpClient = new HttpClient();
        var tokenRequest = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "client_id", _clientId! },
            { "client_secret", _clientSecret! },
            { "code", code },
            { "redirect_uri", Constants.Jira.CallbackUrl }
        };

        var tokenResponse = await httpClient.PostAsync(Constants.Jira.TokenUrl, new FormUrlEncodedContent(tokenRequest));
        var tokenResponseBody = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            return false;
        }

        ParseAndStoreTokens(tokenResponseBody);
        return true;
    }

    public async Task<bool> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken) || !HasCredentials)
        {
            return false;
        }

        try
        {
            using var httpClient = new HttpClient();
            var tokenRequest = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "client_id", _clientId! },
                { "client_secret", _clientSecret! },
                { "refresh_token", _refreshToken }
            };

            var tokenResponse = await httpClient.PostAsync(Constants.Jira.TokenUrl, new FormUrlEncodedContent(tokenRequest));
            var tokenResponseBody = await tokenResponse.Content.ReadAsStringAsync();

            if (!tokenResponse.IsSuccessStatusCode)
            {
                Debug.WriteLine($"Token refresh failed: {tokenResponseBody}");
                return false;
            }

            ParseAndStoreTokens(tokenResponseBody);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Token refresh exception: {ex.Message}");
            return false;
        }
    }

    private void ParseAndStoreTokens(string tokenResponseBody)
    {
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenResponseBody);
        _accessToken = tokenData.GetProperty("access_token").GetString();

        if (tokenData.TryGetProperty("refresh_token", out var newRefreshToken))
        {
            _refreshToken = newRefreshToken.GetString();
        }

        var expiresIn = tokenData.GetProperty("expires_in").GetInt32();
        _tokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn - 60);

        SaveTokens();
    }

    public async Task<bool> EnsureValidTokenAsync()
    {
        if (!IsAuthenticated)
        {
            return false;
        }

        if (IsTokenExpired && HasRefreshToken)
        {
            return await RefreshTokenAsync();
        }

        return !IsTokenExpired;
    }

    private async Task FetchCloudIdAsync()
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        var resourcesResponse = await httpClient.GetAsync("https://api.atlassian.com/oauth/token/accessible-resources");
        if (!resourcesResponse.IsSuccessStatusCode) return;

        var resourcesBody = await resourcesResponse.Content.ReadAsStringAsync();
        var resources = JsonSerializer.Deserialize<JsonElement>(resourcesBody);

        foreach (var resource in resources.EnumerateArray())
        {
            var url = resource.GetProperty("url").GetString();
            if (url == Constants.Jira.CloudInstanceUrl)
            {
                _cloudId = resource.GetProperty("id").GetString();
                SaveTokens();
                break;
            }
        }
    }

    public void SaveRecentIssue(string issueKey)
    {
        try
        {
            var settings = Properties.Settings.Default;
            var recentKeys = settings.RecentJiraIssues?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                ?? new List<string>();

            recentKeys.Remove(issueKey);
            recentKeys.Insert(0, issueKey);

            settings.RecentJiraIssues = string.Join(",", recentKeys.Take(5));
            settings.Save();
        }
        catch
        {
            // Ignore errors
        }
    }

    public List<string> GetRecentIssueKeys()
    {
        var settings = Properties.Settings.Default;
        return settings.RecentJiraIssues?.Split(',', StringSplitOptions.RemoveEmptyEntries).Take(5).ToList()
            ?? new List<string>();
    }

    private void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Silent fail
        }
    }

    private void SendResponse(HttpListenerResponse response, string message)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Authorization Result</title>
    <style>
        body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; }}
        .message {{ font-size: 18px; color: #333; }}
    </style>
</head>
<body>
    <div class='message'>{message}</div>
</body>
</html>";

        var buffer = Encoding.UTF8.GetBytes(html);
        response.ContentType = "text/html";
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private void LoadStoredCredentialsAndTokens()
    {
        try
        {
            var settings = Properties.Settings.Default;
            _clientId = settings.JiraClientId;
            _clientSecret = settings.JiraClientSecret;
            _accessToken = settings.JiraAccessToken;
            _refreshToken = settings.JiraRefreshToken;
            _cloudId = settings.JiraCloudId;

            if (!string.IsNullOrEmpty(settings.JiraTokenExpiry) &&
                DateTime.TryParse(settings.JiraTokenExpiry, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiry))
            {
                _tokenExpiresAtUtc = expiry.ToUniversalTime();
            }
        }
        catch
        {
            // Ignore errors loading tokens
        }
    }

    private void SaveCredentials()
    {
        try
        {
            var settings = Properties.Settings.Default;
            settings.JiraClientId = _clientId ?? "";
            settings.JiraClientSecret = _clientSecret ?? "";
            settings.Save();
        }
        catch
        {
            // Ignore errors saving credentials
        }
    }

    private void SaveTokens()
    {
        try
        {
            var settings = Properties.Settings.Default;
            settings.JiraAccessToken = _accessToken ?? "";
            settings.JiraRefreshToken = _refreshToken ?? "";
            settings.JiraCloudId = _cloudId ?? "";
            settings.JiraTokenExpiry = _tokenExpiresAtUtc.ToString("O");
            settings.Save();
        }
        catch
        {
            // Ignore errors saving tokens
        }
    }

    public void Dispose()
    {
        _listener?.Stop();
        _listener?.Close();
    }
}
