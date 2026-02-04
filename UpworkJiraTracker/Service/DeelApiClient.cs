using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using UpworkJiraTracker.Model;

namespace UpworkJiraTracker.Service;

/// <summary>
/// HTTP client for Deel API calls using cookies from authenticated browser session
/// </summary>
public class DeelApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookieContainer;
    private string? _authToken;
    private string? _pageUrl;
    private string? _contractId;
    private static int _requestCounter = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DeelApiClient()
    {
        _cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(Constants.Deel.BaseUrl)
        };

        // Set common headers to mimic browser
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
        _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.9));
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36");

        // Deel-specific headers
        _httpClient.DefaultRequestHeaders.Add("x-api-version", "2");
        _httpClient.DefaultRequestHeaders.Add("x-app-host", "app.deel.com");
        _httpClient.DefaultRequestHeaders.Add("x-platform", "web");
        _httpClient.DefaultRequestHeaders.Add("x-locale", "en");
        _httpClient.DefaultRequestHeaders.Add("x-owner", "time-tracking");
        _httpClient.DefaultRequestHeaders.Add("x-client-version", "1.30.1410");
        _httpClient.DefaultRequestHeaders.Add("x-client-version-hash", "37b7b01c6374ed02f1f14d5926ae342cd1d8ae4c");
        _httpClient.DefaultRequestHeaders.Add("x-device-dimensions", "{\"height\":720,\"width\":650}");

        // Security headers
        _httpClient.DefaultRequestHeaders.Add("sec-fetch-dest", "empty");
        _httpClient.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
        _httpClient.DefaultRequestHeaders.Add("sec-fetch-site", "same-origin");
        _httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Chromium\";v=\"144\", \"Google Chrome\";v=\"144\"");
        _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
        _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
    }

    /// <summary>
    /// Set cookies from WebView2 session
    /// </summary>
    public void SetCookies(IEnumerable<Cookie> cookies)
    {
        foreach (var cookie in cookies)
        {
            _cookieContainer.Add(new Uri(Constants.Deel.BaseUrl), cookie);

            // Extract auth-token for x-auth-token header
            if (cookie.Name == "auth-token")
            {
                _authToken = cookie.Value;
                Debug.WriteLine($"[DeelApiClient] Found auth-token: {_authToken?.Substring(0, Math.Min(50, _authToken?.Length ?? 0))}...");
            }
        }

        Debug.WriteLine($"[DeelApiClient] Set {_cookieContainer.Count} cookies, auth-token: {(_authToken != null ? "present" : "missing")}");
    }

    /// <summary>
    /// Set the current page URL (used for Referer and x-react-pathname headers)
    /// </summary>
    public void SetPageUrl(string pageUrl)
    {
        _pageUrl = pageUrl;
        Debug.WriteLine($"[DeelApiClient] Page URL set: {pageUrl}");

        // Try to extract contract ID from URL (e.g., /time-attendance/m4pxdjx)
        var uri = new Uri(pageUrl);
        var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length >= 2 && pathParts[0] == "time-attendance")
        {
            _contractId = pathParts[1];
            Debug.WriteLine($"[DeelApiClient] Extracted contract ID: {_contractId}");
        }
    }

    /// <summary>
    /// Prepare request with dynamic headers
    /// </summary>
    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
    {
        var request = new HttpRequestMessage(method, endpoint);

        // Add dynamic headers
        var requestId = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{_requestCounter++}";
        request.Headers.Add("x-request-id", requestId);
        request.Headers.Add("x-started-at", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());

        // Add auth token
        if (!string.IsNullOrEmpty(_authToken))
        {
            request.Headers.Add("x-auth-token", _authToken);
        }

        // Add referer and pathname from page URL
        if (!string.IsNullOrEmpty(_pageUrl))
        {
            request.Headers.Referrer = new Uri(_pageUrl);
            var uri = new Uri(_pageUrl);
            request.Headers.Add("x-react-pathname", uri.AbsolutePath);
        }

        return request;
    }

    /// <summary>
    /// GET /deelapi/time_tracking/profiles/contracts - Get list of contracts
    /// </summary>
    public async Task<DeelApiResponse<DeelContractsResponse>?> GetContractsAsync()
    {
        try
        {
            Debug.WriteLine("[DeelApiClient] Getting contracts...");

            var endpoint = "/deelapi/time_tracking/profiles/contracts";
            var request = CreateRequest(HttpMethod.Get, endpoint);
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"[DeelApiClient] Endpoint: {endpoint}");
            Debug.WriteLine($"[DeelApiClient] Response status: {response.StatusCode}");
            Debug.WriteLine($"[DeelApiClient] Response: {content.Substring(0, Math.Min(500, content.Length))}");

            if (!response.IsSuccessStatusCode)
            {
                return new DeelApiResponse<DeelContractsResponse>
                {
                    Success = false,
                    Error = $"HTTP {(int)response.StatusCode}: {content.Substring(0, Math.Min(200, content.Length))}"
                };
            }

            var data = JsonSerializer.Deserialize<DeelContractsResponse>(content, JsonOptions);
            return new DeelApiResponse<DeelContractsResponse>
            {
                Success = true,
                Data = data
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeelApiClient] Error: {ex.Message}");
            return new DeelApiResponse<DeelContractsResponse>
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// POST /deelapi/time_tracking/time_sheets/shifts - Log time
    /// </summary>
    public async Task<DeelApiResponse<DeelLogShiftResponse>?> LogShiftAsync(string contractOid, double totalWorkedHours, string description, DateTime? date = null)
    {
        try
        {
            var logDate = date ?? DateTime.Today;
            // Format as ISO 8601 with midnight UTC
            var startDate = logDate.ToString("yyyy-MM-dd") + "T00:00:00.000Z";

            Debug.WriteLine($"[DeelApiClient] Logging shift: {totalWorkedHours}h for contract {contractOid} on {startDate}");

            var payload = new DeelLogShiftRequest
            {
                ContractOid = contractOid,
                Start = startDate,
                TotalWorkedHours = totalWorkedHours,
                Description = description,
                Type = "BULK",
                SubmitType = "BULK",
                HourlyReportPresetId = "default",
                IsAutoApproved = false,
                Origin = "PLATFORM",
                WorkLocation = null,
                WorkLocationEntityAddressId = null,
                ShiftType = "UNSPECIFIED",
                IsForecastEdit = false
            };

            var jsonContent = JsonSerializer.Serialize(payload, JsonOptions);
            Debug.WriteLine($"[DeelApiClient] Request payload: {jsonContent}");

            var endpoint = "/deelapi/time_tracking/time_sheets/shifts";
            var request = CreateRequest(HttpMethod.Post, endpoint);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"[DeelApiClient] Endpoint: {endpoint}");
            Debug.WriteLine($"[DeelApiClient] Response status: {response.StatusCode}");
            Debug.WriteLine($"[DeelApiClient] Response: {content.Substring(0, Math.Min(500, content.Length))}");

            if (!response.IsSuccessStatusCode)
            {
                return new DeelApiResponse<DeelLogShiftResponse>
                {
                    Success = false,
                    Error = $"HTTP {(int)response.StatusCode}: {content.Substring(0, Math.Min(500, content.Length))}"
                };
            }

            var data = JsonSerializer.Deserialize<DeelLogShiftResponse>(content, JsonOptions);
            return new DeelApiResponse<DeelLogShiftResponse>
            {
                Success = true,
                Data = data
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeelApiClient] Error: {ex.Message}");
            return new DeelApiResponse<DeelLogShiftResponse>
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// GET /deelapi/contracts/{contractId} - Get contract details including totals and payment cycles
    /// </summary>
    public async Task<DeelApiResponse<DeelContractDetailsResponse>?> GetContractDetailsAsync(string contractId)
    {
        try
        {
            Debug.WriteLine($"[DeelApiClient] Getting contract details for {contractId}...");

            var endpoint = $"/deelapi/contracts/{contractId}";
            var request = CreateRequest(HttpMethod.Get, endpoint);
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"[DeelApiClient] Endpoint: {endpoint}");
            Debug.WriteLine($"[DeelApiClient] Response status: {response.StatusCode}");
            Debug.WriteLine($"[DeelApiClient] Response length: {content.Length}");

            if (!response.IsSuccessStatusCode)
            {
                return new DeelApiResponse<DeelContractDetailsResponse>
                {
                    Success = false,
                    Error = $"HTTP {(int)response.StatusCode}: {content.Substring(0, Math.Min(200, content.Length))}"
                };
            }

            var data = JsonSerializer.Deserialize<DeelContractDetailsResponse>(content, JsonOptions);
            return new DeelApiResponse<DeelContractDetailsResponse>
            {
                Success = true,
                Data = data
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeelApiClient] Error: {ex.Message}");
            return new DeelApiResponse<DeelContractDetailsResponse>
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Full workflow: Get contracts, log time, get updated details
    /// </summary>
    public async Task<DeelLogHoursResult> LogHoursFullWorkflowAsync(int hours, int minutes, string description, DateTime? date = null)
    {
        var result = new DeelLogHoursResult();

        try
        {
            // Step 1: Get contracts to verify API and get contract ID
            Debug.WriteLine("[DeelApiClient] Step 1: Getting contracts...");
            var contractsResponse = await GetContractsAsync();
            if (contractsResponse?.Success != true || contractsResponse.Data?.Contracts == null || contractsResponse.Data.Contracts.Count == 0)
            {
                result.Error = contractsResponse?.Error ?? "No contracts found";
                return result;
            }

            var contract = contractsResponse.Data.Contracts.First();
            result.ContractId = contract.ContractId;
            result.ContractName = contract.Name;
            Debug.WriteLine($"[DeelApiClient] Found contract: {contract.ContractId} - {contract.Name}");

            // Step 2: Log time
            Debug.WriteLine("[DeelApiClient] Step 2: Logging time...");
            double totalWorkedHours = hours + (minutes / 60.0);
            var shiftResponse = await LogShiftAsync(contract.ContractId, totalWorkedHours, description, date);
            if (shiftResponse?.Success != true || shiftResponse.Data?.Shift == null)
            {
                result.Error = shiftResponse?.Error ?? "Failed to log shift";
                return result;
            }

            var shift = shiftResponse.Data.Shift;
            result.ShiftId = shift.PublicId;
            result.LoggedHours = shift.TotalWorkedHours;
            result.CalculatedPayment = shift.CalculatedPayment;
            result.ShiftStatus = shift.Status;
            Debug.WriteLine($"[DeelApiClient] Shift logged: {shift.PublicId}, Payment: {shift.CalculatedPayment}");

            // Step 3: Get contract details to retrieve updated totals
            Debug.WriteLine("[DeelApiClient] Step 3: Getting contract details...");
            var detailsResponse = await GetContractDetailsAsync(contract.ContractId);
            if (detailsResponse?.Success == true && detailsResponse.Data != null)
            {
                result.Total = detailsResponse.Data.Total;
                result.PaymentCycles = detailsResponse.Data.PaymentCycles;
                result.HourlyRate = detailsResponse.Data.TimeTracking?.HourlyRate;
                result.Currency = detailsResponse.Data.Currency;
                Debug.WriteLine($"[DeelApiClient] Contract details retrieved. Total: {result.Total?.Amount}, Currency: {result.Currency}");
            }

            result.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeelApiClient] Workflow error: {ex.Message}");
            result.Error = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Test API connectivity by fetching contracts
    /// </summary>
    public async Task<(bool Success, string? ContractId, string? Error, string? RawResponse)> TestConnectionAsync()
    {
        var result = await GetContractsAsync();

        if (result?.Success == true && result.Data?.Contracts != null && result.Data.Contracts.Count > 0)
        {
            var contract = result.Data.Contracts.First();
            var rawJson = JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
            return (true, contract.ContractId, null, rawJson);
        }

        return (false, null, result?.Error ?? "Unknown error", null);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

/// <summary>
/// Generic API response wrapper
/// </summary>
public class DeelApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result of the full log hours workflow
/// </summary>
public class DeelLogHoursResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }

    // Contract info
    public string? ContractId { get; set; }
    public string? ContractName { get; set; }

    // Shift info
    public string? ShiftId { get; set; }
    public string? LoggedHours { get; set; }
    public string? CalculatedPayment { get; set; }
    public string? ShiftStatus { get; set; }

    // Contract details
    public string? HourlyRate { get; set; }
    public string? Currency { get; set; }
    public DeelTotal? Total { get; set; }
    public List<DeelPaymentCycle>? PaymentCycles { get; set; }
}
