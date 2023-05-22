namespace ProcRoll
{
    public class ProcessStartInfo
    {
        public string? FileName { get; set; }
        public string? Arguments { get; set; }
        public StartMode? StartMode { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
        public Action<string>? StdOut { get; set; }
        public Action<string>? StdErr { get; set; }
        public string? StartedStringMatch { get; set; }
        public List<string> DependsOn { get; set; } = new();
        public StopMethod? StopMethod { get; set; }
        public Dictionary<string, string> AutoResponses { get; set; } = new();
    }

    public enum StartMode
    {
        Default = 0,
        Background = 1,
        Hosted = 2
    }

    public enum StopMethod
    {
        Default = 0,
        CtrlC = 1
    }
}
