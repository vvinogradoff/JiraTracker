using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using UpworkJiraTracker.Model;
using UpworkJiraTracker.Model.EventArgs;

namespace UpworkJiraTracker.Service;

public class JiraIssuesService
{
    private readonly JiraOAuthService _authService;
    private readonly UpworkIntegrationFlaUI _upworkIntegration;
    private readonly JiraIssueCacheService _cacheService;
    private readonly HttpClient _httpClient;

    // Cache for default suggestions (Recent, My Issues, New sections)
    private List<JiraIssue>? _cachedDefaultSuggestions;

    public event EventHandler<SuggestionsUpdatedEventArgs>? SuggestionsUpdated;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler<WorklogResult>? WorklogCompleted;

    public bool IsAuthenticated => _authService.IsAuthenticated;

    /// <summary>
    /// Returns true if the cache has issues and search should be instant (no debounce needed)
    /// </summary>
    public bool HasCachedIssues => _cacheService.HasCachedIssues;

    public JiraIssuesService(JiraOAuthService authService, UpworkIntegrationFlaUI upworkIntegration, JiraIssueCacheService cacheService)
    {
        _authService = authService;
        _upworkIntegration = upworkIntegration;
        _cacheService = cacheService;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Loads default suggestions with Recent, My Issues, and New sections.
    /// Uses cache service when available for Recent/My Issues data.
    /// </summary>
    public async Task LoadDefaultSuggestionsAsync()
    {
        if (!_authService.IsAuthenticated)
        {
            // Show "Not connected to Jira" placeholder
            var placeholder = new List<JiraIssue>
            {
                new JiraIssue
                {
                    Key = "",
                    Summary = "Not connected to Jira",
                    IsSectionHeader = true, // Use header styling (gray/inactive)
                    Status = "",
                    Assignee = "",
                    Reporter = ""
                }
            };
            SuggestionsUpdated?.Invoke(this, new SuggestionsUpdatedEventArgs(placeholder, isCached: false));
            return;
        }

        // Show cached results immediately if available
        if (_cachedDefaultSuggestions != null && _cachedDefaultSuggestions.Count > 0)
        {
            SuggestionsUpdated?.Invoke(this, new SuggestionsUpdatedEventArgs(_cachedDefaultSuggestions, isCached: false));
        }
        else
        {
            LoadingStateChanged?.Invoke(this, true);
        }

        try
        {
            var suggestions = new List<JiraIssue>();
            var existingKeys = new HashSet<string>();

            // Recent issues - try to get from cache first
            var recentKeys = _authService.GetRecentIssueKeys();
            var recent = new List<JiraIssue>();

            foreach (var key in recentKeys)
            {
                var cachedIssue = _cacheService.GetIssue(key);
                if (cachedIssue != null)
                {
                    recent.Add(cachedIssue);
                }
            }

            // If we didn't get all recent from cache, fetch from API
            if (recent.Count < recentKeys.Count)
            {
                recent = await GetRecentIssuesAsync();
                _cacheService.AddToCache(recent);
            }

            if (recent.Count > 0)
            {
                suggestions.Add(JiraIssue.CreateSectionHeader("Recent"));
                foreach (var issue in recent)
                {
                    issue.Section = "Recent";
                    suggestions.Add(issue);
                    existingKeys.Add(issue.Key);
                }
            }

            // My issues (assigned to me in open sprints)
            var myIssues = await GetMyIssuesAsync(existingKeys);
            _cacheService.AddToCache(myIssues);

            if (myIssues.Count > 0)
            {
                suggestions.Add(JiraIssue.CreateSectionHeader("My Issues"));
                foreach (var issue in myIssues)
                {
                    issue.Section = "My Issues";
                    suggestions.Add(issue);
                    existingKeys.Add(issue.Key);
                }
            }

            // New issues (created or reported by me)
            var newIssues = await GetNewIssuesAsync(existingKeys);
            _cacheService.AddToCache(newIssues);

            if (newIssues.Count > 0)
            {
                suggestions.Add(JiraIssue.CreateSectionHeader("New"));
                foreach (var issue in newIssues)
                {
                    issue.Section = "New";
                    suggestions.Add(issue);
                }
            }

            // Update cache and fire event
            _cachedDefaultSuggestions = suggestions;

            // If no suggestions at all, show "Nothing found" placeholder
            if (suggestions.Count == 0)
            {
                suggestions = new List<JiraIssue>
                {
                    new JiraIssue
                    {
                        Key = "",
                        Summary = "Nothing found",
                        IsSectionHeader = true,
                        Status = "",
                        Assignee = "",
                        Reporter = ""
                    }
                };
            }

            SuggestionsUpdated?.Invoke(this, new SuggestionsUpdatedEventArgs(suggestions, isCached: false));
        }
        catch
        {
            // If we have cache, keep showing it; otherwise show "Nothing found"
            if (_cachedDefaultSuggestions == null || _cachedDefaultSuggestions.Count == 0)
            {
                var placeholder = new List<JiraIssue>
                {
                    new JiraIssue
                    {
                        Key = "",
                        Summary = "Nothing found",
                        IsSectionHeader = true,
                        Status = "",
                        Assignee = "",
                        Reporter = ""
                    }
                };
                SuggestionsUpdated?.Invoke(this, new SuggestionsUpdatedEventArgs(placeholder, isCached: false));
            }
        }
        finally
        {
            LoadingStateChanged?.Invoke(this, false);
        }
    }

    /// <summary>
    /// Searches issues from cache instantly. Returns results without debounce.
    /// Call this when cache has issues (HasCachedIssues == true).
    /// </summary>
    public bool SearchFromCache(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            // For empty text, show cached default suggestions or trigger load
            if (_cachedDefaultSuggestions != null && _cachedDefaultSuggestions.Count > 0)
            {
                SuggestionsUpdated?.Invoke(this, new SuggestionsUpdatedEventArgs(_cachedDefaultSuggestions, isCached: false));
            }
            else
            {
                // Trigger async load but don't await
                _ = LoadDefaultSuggestionsAsync();
            }
			return true;
        }

        var results = _cacheService.Search(text);

        // If no results found, show "Nothing found" placeholder (not cached)
        if (results.Count == 0)
        {
            results = new List<JiraIssue>
            {
                new JiraIssue
                {
                    Key = "",
                    Summary = Constants.UI.NothingFound,
                    IsSectionHeader = true,
                    Status = "",
                    Assignee = "",
                    Reporter = ""
                }
            };
            SuggestionsUpdated?.Invoke(this, new SuggestionsUpdatedEventArgs(results, isCached: true));
            return false;
        }

        // Results from cache - mark as cached
        SuggestionsUpdated?.Invoke(this, new SuggestionsUpdatedEventArgs(results, isCached: true));
		return true;
    }

    /// <summary>
    /// Searches issues from Jira API. Use this when cache is empty (with debounce).
    /// Results are added to the cache.
    /// </summary>
    public async Task SearchFromApiAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            await LoadDefaultSuggestionsAsync();
            return;
        }

        if (!_authService.IsAuthenticated)
        {
            // Show "Not connected to Jira" placeholder
            var placeholder = new List<JiraIssue>
            {
                new JiraIssue
                {
                    Key = "",
                    Summary = "Not connected to Jira",
                    IsSectionHeader = true,
                    Status = "",
                    Assignee = "",
                    Reporter = ""
                }
            };
            SuggestionsUpdated?.Invoke(this, new SuggestionsUpdatedEventArgs(placeholder, isCached: false));
            return;
        }

        try
        {
            LoadingStateChanged?.Invoke(this, true);
            var jql = $"(key ~ '{text}' OR summary ~ '{text}') ORDER BY updated DESC";
            var issues = await SearchIssuesAsync(jql, 20);

            // Add results to cache for future searches
            _cacheService.AddToCache(issues);

            // If no results found, show "Nothing found" placeholder
            if (issues.Count == 0)
            {
                issues = new List<JiraIssue>
                {
                    new JiraIssue
                    {
                        Key = "",
                        Summary = Constants.UI.NothingFound,
                        IsSectionHeader = true,
                        Status = "",
                        Assignee = "",
                        Reporter = ""
                    }
                };
            }

            // API results - not cached
            SuggestionsUpdated?.Invoke(this, new SuggestionsUpdatedEventArgs(issues, isCached: false));
        }
        catch
        {
            // Show "Nothing found" on error
            var placeholder = new List<JiraIssue>
            {
                new JiraIssue
                {
                    Key = "",
                    Summary = Constants.UI.NothingFound,
                    IsSectionHeader = true,
                    Status = "",
                    Assignee = "",
                    Reporter = ""
                }
            };
            SuggestionsUpdated?.Invoke(this, new SuggestionsUpdatedEventArgs(placeholder, isCached: false));
        }
        finally
        {
            LoadingStateChanged?.Invoke(this, false);
        }
    }

    public void SelectIssue(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        _authService.SaveRecentIssue(key);
        _upworkIntegration.UpdateMemo(key);

        // Invalidate default suggestions cache since recent list changed
        _cachedDefaultSuggestions = null;
    }

    public async Task<bool> LogTimeAsync(string issueKey, TimeSpan timeSpent, string? comment = null, double? remainingEstimateHours = null, string? issueSummary = null)
    {
        if (!_authService.IsAuthenticated || string.IsNullOrEmpty(_authService.CloudId))
        {
            WorklogCompleted?.Invoke(this, new WorklogResult
            {
                Success = false,
                IssueKey = issueKey,
                ErrorMessage = "Not authenticated",
                Comment = comment,
                IssueSummary = issueSummary
            });
            return false;
        }

        // Build worklog URL with optional remaining estimate adjustment
        var worklogUrl = $"https://api.atlassian.com/ex/jira/{_authService.CloudId}/rest/api/3/issue/{issueKey}/worklog";

        if (remainingEstimateHours.HasValue && remainingEstimateHours.Value >= 0)
        {
            // Convert hours to Jira time format (e.g., "2h", "2h 30m", "0m" for zero)
            var totalMinutes = (int)Math.Round(remainingEstimateHours.Value * 60);
            var hours = totalMinutes / 60;
            var minutes = totalMinutes % 60;

            string newEstimate;
            if (totalMinutes == 0)
            {
                newEstimate = "0m";
            }
            else if (hours > 0 && minutes > 0)
            {
                newEstimate = $"{hours}h {minutes}m";
            }
            else if (hours > 0)
            {
                newEstimate = $"{hours}h";
            }
            else
            {
                newEstimate = $"{minutes}m";
            }

            worklogUrl += $"?adjustEstimate=new&newEstimate={Uri.EscapeDataString(newEstimate)}";
            System.Diagnostics.Debug.WriteLine($"Logging worklog with remaining estimate: {newEstimate} (from {remainingEstimateHours.Value} hours = {totalMinutes} minutes)");
            System.Diagnostics.Debug.WriteLine($"Worklog URL: {worklogUrl}");
        }
        else if (remainingEstimateHours.HasValue)
        {
            System.Diagnostics.Debug.WriteLine($"Skipping remaining estimate - negative value: {remainingEstimateHours.Value}");
        }

        // Build worklog data
        object worklogData;
        if (!string.IsNullOrWhiteSpace(comment))
        {
            // Jira API uses Atlassian Document Format for comments
            worklogData = new
            {
                timeSpentSeconds = (int)timeSpent.TotalSeconds,
                comment = new
                {
                    type = "doc",
                    version = 1,
                    content = new[]
                    {
                        new
                        {
                            type = "paragraph",
                            content = new[]
                            {
                                new { type = "text", text = comment }
                            }
                        }
                    }
                }
            };
        }
        else
        {
            worklogData = new
            {
                timeSpentSeconds = (int)timeSpent.TotalSeconds
            };
        }

        var jsonContent = JsonSerializer.Serialize(worklogData);
        var success = await ExecutePostWithRetryAsync(worklogUrl, jsonContent);

        var result = new WorklogResult
        {
            Success = success,
            IssueKey = issueKey,
            TimeLogged = timeSpent,
            ErrorMessage = success ? null : "Failed to log time",
            Comment = comment,
            IssueSummary = issueSummary,
            RemainingEstimateHours = remainingEstimateHours
        };

        WorklogCompleted?.Invoke(this, result);
        return success;
    }

    private async Task<List<JiraIssue>> GetRecentIssuesAsync()
    {
        var recentKeys = _authService.GetRecentIssueKeys();
        if (recentKeys.Count == 0) return new List<JiraIssue>();

        var jql = $"key in ({string.Join(",", recentKeys)}) ORDER BY created DESC";
        return await SearchIssuesAsync(jql, 5);
    }

    private async Task<List<JiraIssue>> GetNewIssuesAsync(IEnumerable<string> keysToExlude)
    {
		var jql = $"key not in ({string.Join(",", keysToExlude)}) (reporter = currentUser() OR assignee = currentUser()) ORDER BY created DESC";
        return await SearchIssuesAsync(jql, 5);
    }

    private async Task<List<JiraIssue>> GetMyIssuesAsync(IEnumerable<string> keysToExlude)
    {
        var jql = "assignee = currentUser() AND sprint in openSprints() ORDER BY updated DESC";
        var issues = await SearchIssuesAsync(jql, 10);

        if (issues.Count == 0)
        {
            jql = "assignee = currentUser() ORDER BY updated DESC";
            issues = await SearchIssuesAsync(jql, 10);
        }

        return issues;
    }

    private async Task<List<JiraIssue>> SearchIssuesAsync(string jqlQuery, int maxResults)
    {
        if (!_authService.IsAuthenticated || string.IsNullOrEmpty(_authService.CloudId))
        {
            return new List<JiraIssue>();
        }

        var searchUrl = $"https://api.atlassian.com/ex/jira/{_authService.CloudId}/rest/api/3/search/jql?" +
            $"jql={Uri.EscapeDataString(jqlQuery)}&" +
            $"maxResults={maxResults}&" +
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
                System.Diagnostics.Debug.WriteLine($"Got 401, attempting token refresh (attempt {attempt + 1}/{Constants.Jira.MaxRetries})");

                if (!await _authService.RefreshTokenAsync())
                {
                    System.Diagnostics.Debug.WriteLine("Token refresh failed");
                    return null;
                }

                continue;
            }

            // For other errors, don't retry
            System.Diagnostics.Debug.WriteLine($"Jira API error: {response.StatusCode}");
            return null;
        }

        System.Diagnostics.Debug.WriteLine("Max retries exceeded");
        return null;
    }

    private async Task<bool> ExecutePostWithRetryAsync(string url, string jsonContent)
    {
        for (int attempt = 0; attempt < Constants.Jira.MaxRetries; attempt++)
        {
            // Ensure token is valid before request
            if (_authService.IsTokenExpired)
            {
                if (!await _authService.RefreshTokenAsync())
                {
                    return false;
                }
            }

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authService.AccessToken);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"Worklog posted successfully to {url}");
                return true;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                System.Diagnostics.Debug.WriteLine($"Got 401 on POST, attempting token refresh (attempt {attempt + 1}/{Constants.Jira.MaxRetries})");

                if (!await _authService.RefreshTokenAsync())
                {
                    System.Diagnostics.Debug.WriteLine("Token refresh failed");
                    return false;
                }

                continue;
            }

            // Log error details for debugging
            var errorBody = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"Jira POST error: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"Error body: {errorBody}");
            System.Diagnostics.Debug.WriteLine($"Request URL: {url}");
            System.Diagnostics.Debug.WriteLine($"Request body: {jsonContent}");
            return false;
        }

        System.Diagnostics.Debug.WriteLine("Max retries exceeded for POST");
        return false;
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
            System.Diagnostics.Debug.WriteLine($"Error parsing Jira response: {ex.Message}");
        }

        return result;
    }
}
