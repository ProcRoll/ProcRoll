namespace ProcRoll;

/// <summary>
/// Method needed to stop the external process.
/// </summary>
public enum StopMethod
{
    /// <summary>
    /// Will kill the external process.
    /// </summary>
    Default = 0,
    /// <summary>
    /// Will send Ctrl+C to the process.
    /// </summary>
    CtrlC = 1,
    /// <summary>
    /// Will send Ctrl+Break to the process.
    /// </summary>
    CtrlBreak = 2
}
