namespace ProcRoll
{
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
        /// When used with hosting, controls when process is started.
        /// <c>Hosted</c> will be started automatically as a background worker.
        /// <c>Background</c> will start and stop the service as required by dependency injection and dependencies from other processes.
        /// <c>Default</c> requires manual starting.
        /// </summary>
        public StartMode? StartMode { get; set; }
        /// <summary>
        /// Environment variable values to overwrite or add to the default environment variables for the external process.
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
        /// <summary>
        /// Action to receive the console standard output messages of the external process.
        /// </summary>
        public Action<string>? StdOut { get; set; }
        /// <summary>
        /// Action to receive the console standard error messages of the external process.
        /// </summary>
        public Action<string>? StdErr { get; set; }
        /// <summary>
        /// A <see cref="System.Text.RegularExpressions.Regex"/> query to identify standard output message that indicates the external process has fully started.
        /// </summary>
        public string? StartedStringMatch { get; set; }
        /// <summary>
        /// A list of names of ProcRoll configurations that are required to be started before this process.
        /// </summary>
        public List<string> DependsOn { get; set; } = new();
        /// <summary>
        /// Method needed to stop the external process.
        /// <c>CtrlC</c> will send Ctrl+C to the process.
        /// <c>Default</c> will kill the external process.
        /// </summary>
        public StopMethod? StopMethod { get; set; }
        /// <summary>
        /// A list of queries with matching response text for automatically controlling the external application through the console.
        /// </summary>
        public Dictionary<string, string> AutoResponses { get; set; } = new();
    }

    /// <summary>
    /// Controls when process is started when using hosting.
    /// </summary>
    public enum StartMode
    {
        /// <summary>
        /// Requires manual starting.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Will start and stop the service as required by dependency injection and dependencies from other processes.
        /// </summary>
        Background = 1,
        /// <summary>
        /// Will be started automatically as a background worker.
        /// </summary>
        Hosted = 2
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
        CtrlC = 1
    }
}
