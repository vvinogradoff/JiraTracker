namespace UpworkJiraTracker.Model;

public class JiraIssue
{
    public string Key { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Status { get; set; } = "";
    public string Assignee { get; set; } = "";
    public string Reporter { get; set; } = "";
    public string Section { get; set; } = "";
    public bool IsSectionHeader { get; set; } = false;

    /// <summary>
    /// Returns a searchable string combining key, summary, assignee, and reporter
    /// </summary>
    public string GetSearchableText() => $"{Key} {Summary} {Assignee} {Reporter}";

    public override string ToString() => $"{Key}: {Summary}";

    public static JiraIssue CreateSectionHeader(string sectionName) => new()
    {
        Key = sectionName,
        IsSectionHeader = true,
        Section = sectionName
    };
}
