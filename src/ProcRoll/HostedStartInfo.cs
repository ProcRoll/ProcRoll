namespace ProcRoll;

/// <summary>
/// 
/// </summary>
public class HostedStartInfo : ProcessStartInfo
{
    /// <summary>
    /// When used with hosting, controls when process is started.
    /// <c>Hosted</c> will be started automatically as a background worker.
    /// <c>Background</c> will start and stop the service as required by dependency injection and dependencies from other processes.
    /// <c>Default</c> requires manual starting.
    /// </summary>
    public StartMode? StartMode { get; set; }
    /// <summary>
    /// A list of names of ProcRoll configurations that are required to be started before this process.
    /// </summary>
    public List<string> DependsOn { get; set; } = new();
    /// <summary>
    /// 
    /// </summary>
    public string? Handler { get; set; }
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
