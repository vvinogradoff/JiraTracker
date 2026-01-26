namespace UpworkJiraTracker.Model;

public class WorklogResult
{
	public bool Success { get; set; }
	public string IssueKey { get; set; } = "";
	public TimeSpan TimeLogged { get; set; }
	public string? ErrorMessage { get; set; }
}
