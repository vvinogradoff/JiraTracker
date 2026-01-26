namespace UpworkJiraTracker.Model;

public enum UpworkState
{
    /// <summary>
    /// No Upwork process was found
    /// </summary>
    NoProcess,

    /// <summary>
    /// Upwork process found but unable to automate (not started with --enable-features=UiaProvider)
    /// </summary>
    ProcessFoundButCannotAutomate,

    /// <summary>
    /// Upwork process found and successfully automated
    /// </summary>
    FullyAutomated
}
