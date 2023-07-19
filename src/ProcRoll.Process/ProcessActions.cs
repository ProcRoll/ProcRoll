namespace ProcRoll;

/// <summary>
/// Handlers for process events.
/// </summary>
public class ProcessActions
{
    /// <summary>
    /// Action to receive the console standard output messages of the external process.
    /// </summary>
    public Action<string>? StdOut { get; set; }
    /// <summary>
    /// Action to receive the console standard error messages of the external process.
    /// </summary>
    public Action<string>? StdErr { get; set; }
    /// <summary>
    /// Action executed before starting.
    /// </summary>
    public Action? OnStarting { get; set; }
    /// <summary>
    /// Action executed after starting.
    /// </summary>
    public Action? OnStarted { get; set; }
    /// <summary>
    /// Action executed before stopping.
    /// </summary>
    public Action? OnStopping { get; set; }
    /// <summary>
    /// Action executed after stopping.
    /// </summary>
    public Action? OnStopped { get; set; }

}
