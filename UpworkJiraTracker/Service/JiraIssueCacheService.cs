using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Threading;
using UpworkJiraTracker.Model;

namespace UpworkJiraTracker.Service;

/// <summary>
/// Background service that caches Jira issues for fast autocomplete searching.
/// Refreshes every 5 minutes and supports instant word-based searching.
/// </summary>
public class JiraIssueCacheService : IDisposable
{
    private readonly JiraOAuthService _authService;
    private readonly HttpClient _httpClient;
    private readonly DispatcherTimer _refreshTimer;
    private readonly ConcurrentDictionary<string, JiraIssue> _issueCache = new();

    private bool _isRefreshing = false;
    private bool _disposed = false;

    /// <summary>
    /// Returns true if the cache has any issues
    /// </summary>
    public bool HasCachedIssues => !_issueCache.IsEmpty;

    public JiraIssueCacheService(JiraOAuthService authService)
    {
        _authService = authService;
        _httpClient = new HttpClient();

        // Setup refresh timer
        _refreshTimer = new DispatcherTimer
        {
            Interval = Constants.Jira.CacheRefreshInterval
        };
        _refreshTimer.Tick += async (s, e) => await RefreshCacheAsync();
    }

    /// <summary>
    /// Starts the cache service and performs initial refresh
    /// </summary>
    public async Task StartAsync()
    {
        _refreshTimer.Start();
        await RefreshCacheAsync();
    }

    /// <summary>
    /// Stops the cache service
    /// </summary>
    public void Stop()
    {
        _refreshTimer.Stop();
    }

    /// <summary>
    /// Refreshes the cache by fetching all relevant issues from Jira
    /// </summary>
    public async Task RefreshCacheAsync()
    {
        if (_isRefreshing || !_authService.IsAuthenticated)
            return;

        _isRefreshing = true;

        try
        {
            System.Diagnostics.Debug.WriteLine("Starting Jira cache refresh...");

            // Build JQL: all statuses except excluded ones, OR user is reporter/assignee
            var excludedStatuses = string.Join("\", \"", Constants.Jira.ExcludedStatuses);
            var jql = $"(status NOT IN (\"{excludedStatuses}\")) OR reporter = currentUser() OR assignee = currentUser() ORDER BY updated DESC";

            var issues = await FetchAllIssuesAsync(jql);

            if (issues.Count > 0)
            {
                // Clear and repopulate cache
                _issueCache.Clear();
                foreach (var issue in issues)
                {
                    _issueCache.TryAdd(issue.Key, issue);
                }

                System.Diagnostics.Debug.WriteLine($"Jira cache refreshed with {issues.Count} issues");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No issues returned from Jira, keeping existing cache");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Cache refresh failed: {ex.Message}");
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    /// <summary>
    /// Searches the cache for issues matching all words in the search text.
    /// Search is performed against: key, summary, assignee, reporter
    /// </summary>
    public List<JiraIssue> Search(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return _issueCache.Values.Take(20).ToList();

        // Split search text into words
        var searchWords = searchText.ToLowerInvariant()
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (searchWords.Length == 0)
            return _issueCache.Values.Take(20).ToList();

        // Find issues where ALL words match the searchable text
        var results = _issueCache.Values
            .Where(issue =>
            {
                var searchableText = issue.GetSearchableText().ToLowerInvariant();
                return searchWords.All(word => searchableText.Contains(word));
            })
            .OrderByDescending(issue => issue.Key) // Most recent keys first (assuming sequential numbering)
            .Take(20)
            .ToList();

        return results;
    }

    /// <summary>
    /// Adds issues to the cache (e.g., from API search results)
    /// </summary>
    public void AddToCache(IEnumerable<JiraIssue> issues)
    {
        foreach (var issue in issues)
        {
            _issueCache.AddOrUpdate(issue.Key, issue, (_, _) => issue);
        }
    }

    /// <summary>
    /// Gets an issue from the cache by key
    /// </summary>
    public JiraIssue? GetIssue(string key)
    {
        return _issueCache.TryGetValue(key, out var issue) ? issue : null;
    }

    /// <summary>
    /// Clears the cache
    /// </summary>
    public void ClearCache()
    {
        _issueCache.Clear();
    }

    private async Task<List<JiraIssue>> FetchAllIssuesAsync(string jql)
    {
        var allIssues = new List<JiraIssue>();
        var startAt = 0;
        var maxResults = Constants.Jira.CacheMaxIssuesPerQuery;
        var hasMore = true;

        while (hasMore)
        {
            var issues = await SearchIssuesAsync(jql, maxResults, startAt);

            if (issues.Count == 0)
            {
                hasMore = false;
            }
            else
            {
                allIssues.AddRange(issues);
                startAt += issues.Count;

                // Safety limit to prevent infinite loops
                if (allIssues.Count >= 1000 || issues.Count < maxResults)
                {
                    hasMore = false;
                }
            }
        }

        return allIssues;
    }

    private async Task<List<JiraIssue>> SearchIssuesAsync(string jqlQuery, int maxResults, int startAt = 0)
    {
        if (!_authService.IsAuthenticated || string.IsNullOrEmpty(_authService.CloudId))
        {
            return new List<JiraIssue>();
        }

        var searchUrl = $"https://api.atlassian.com/ex/jira/{_authService.CloudId}/rest/api/3/search/jql?" +
            $"jql={Uri.EscapeDataString(jqlQuery)}&" +
            $"maxResults={maxResults}&" +
            $"startAt={startAt}&" +
            $"fields=key,summary,status,assignee,reporter";

        var response = await ExecuteWithRetryAsync(searchUrl);
        if (response == null)
        {
            return new List<JiraIssue>();
        }

        return ParseIssuesFromResponse(response);
    }

    private async Task<string?> ExecuteWithRetryAsync(string url)
    {
        for (int attempt = 0; attempt < Constants.Jira.MaxRetries; attempt++)
        {
            // Ensure token is valid before request
            if (_authService.IsTokenExpired)
            {
                if (!await _authService.RefreshTokenAsync())
                {
                    return null;
                }
            }

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authService.AccessToken);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                System.Diagnostics.Debug.WriteLine($"Got 401 in cache service, attempting token refresh (attempt {attempt + 1}/{Constants.Jira.MaxRetries})");

                if (!await _authService.RefreshTokenAsync())
                {
                    System.Diagnostics.Debug.WriteLine("Token refresh failed in cache service");
                    return null;
                }

                continue;
            }

            // For other errors, don't retry
            System.Diagnostics.Debug.WriteLine($"Jira API error in cache service: {response.StatusCode}");
            return null;
        }

        System.Diagnostics.Debug.WriteLine("Max retries exceeded in cache service");
        return null;
    }

    private List<JiraIssue> ParseIssuesFromResponse(string responseBody)
    {
        var result = new List<JiraIssue>();

        try
        {
            var searchResult = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var issues = searchResult.GetProperty("issues");

            foreach (var issue in issues.EnumerateArray())
            {
                var key = issue.GetProperty("key").GetString() ?? "";
                var fields = issue.GetProperty("fields");

                // Assignee can be null if unassigned
                var assignee = "";
                if (fields.TryGetProperty("assignee", out var assigneeElement) &&
                    assigneeElement.ValueKind != JsonValueKind.Null)
                {
                    assignee = assigneeElement.GetProperty("displayName").GetString() ?? "";
                }

                // Reporter can be null in some cases
                var reporter = "";
                if (fields.TryGetProperty("reporter", out var reporterElement) &&
                    reporterElement.ValueKind != JsonValueKind.Null)
                {
                    reporter = reporterElement.GetProperty("displayName").GetString() ?? "";
                }

                result.Add(new JiraIssue
                {
                    Key = key,
                    Summary = fields.GetProperty("summary").GetString() ?? "",
                    Status = fields.GetProperty("status").GetProperty("name").GetString() ?? "",
                    Assignee = assignee,
                    Reporter = reporter,
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error parsing Jira response in cache service: {ex.Message}");
        }

        return result;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _refreshTimer.Stop();
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}
