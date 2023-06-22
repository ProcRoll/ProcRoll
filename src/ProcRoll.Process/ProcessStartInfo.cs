namespace ProcRoll;

/// <summary>
/// Configuration for starting an external process.
/// </summary>
public class ProcessStartInfo
{
    /// <summary>
    /// Name of the external executable file.
    /// </summary>
    public string? FileName { get; set; }
    /// <summary>
    /// Arguments to pass to the process. 
    /// Can include placeholders using braces <c>{}</c> for input of values when starting the process.
    /// </summary>
    public string? Arguments { get; set; }
    /// <summary>
    /// Environment variable values to overwrite or add to the default environment variables for the external process.
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    /// <summary>
    /// A <see cref="System.Text.RegularExpressions.Regex"/> query to identify standard output message that indicates the external process has fully started.
    /// </summary>
    public string? StartedStringMatch { get; set; }
    /// <summary>
    /// Method needed to stop the external process.
    /// <c>CtrlC</c> will send Ctrl+C to the process.
    /// <c>Default</c> will kill the external process.
    /// </summary>
    public StopMethod? StopMethod { get; set; }
    /// <summary>
    /// Action to receive the console standard output messages of the external process.
    /// </summary>
    public Action<string>? StdOut { get; set; }
    /// <summary>
    /// Action to receive the console standard error messages of the external process.
    /// </summary>
    public Action<string>? StdErr { get; set; }
}

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
