namespace ProcRoll;

/// <summary>
/// Controls when process is started.
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
