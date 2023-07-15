using System.ComponentModel.DataAnnotations;

namespace ProcRoll;

/// <summary>
/// Configuration for starting an external process.
/// </summary>
public class ProcessStartInfo
{
    /// <summary>
    /// Name of the external executable file.
    /// </summary>
    [Required]
    public required string FileName { get; set; }
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
    /// 
    /// </summary>
    public bool UseShellExecute { get; set; } = false;

    /// <summary>
    /// A <see cref="System.Text.RegularExpressions.Regex"/> query to identify standard output message that indicates the external process has fully started.
    /// </summary>
    public string? StartedStringMatch { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public StopMethod StopMethod { get; set; } = StopMethod.Default;
}
