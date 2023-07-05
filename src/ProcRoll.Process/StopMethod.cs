namespace ProcRoll;

/// <summary>
/// Method needed to stop the external process.
/// </summary>
public enum StopMethod
{
    /// <summary>
    /// If process is shelled, will send Ctrl+C to close it,
    /// otherise will kill the process.
    /// </summary>
    Default = 0,
    /// <summary>
    /// Will kill the external process.
    /// </summary>
    Kill = 1,
    /// <summary>
    /// Will close to external process.
    /// </summary>
    Close = 2,
    /// <summary>
    /// Will send Ctrl+C to the process.
    /// </summary>
    CtrlC = 3,
    /// <summary>
    /// Will send Ctrl+Break to the process.
    /// </summary>
    CtrlBreak = 4
}
