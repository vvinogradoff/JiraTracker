namespace UpworkJiraTracker.Model;

public class IssueDetails
{
    public string Key { get; }
    public string Summary { get; }
    public string Assignee { get; }
    public string Reporter { get; }
    public string Status { get; }

    public IssueDetails(JiraIssue issue)
    {
        Key = issue.Key;
        Summary = issue.Summary;
        Assignee = issue.Assignee;
        Reporter = issue.Reporter;
        Status = issue.Status;
    }
}
