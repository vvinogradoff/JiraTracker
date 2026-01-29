namespace UpworkJiraTracker.Model.EventArgs;

public class SuggestionsUpdatedEventArgs : System.EventArgs
{
    /// <summary>
    /// The list of Jira issue suggestions.
    /// </summary>
    public List<JiraIssue> Suggestions { get; }

    /// <summary>
    /// True if results came from the local cache, false if from API search.
    /// </summary>
    public bool IsCached { get; }

    public SuggestionsUpdatedEventArgs(List<JiraIssue> suggestions, bool isCached)
    {
        Suggestions = suggestions;
        IsCached = isCached;
    }
}
