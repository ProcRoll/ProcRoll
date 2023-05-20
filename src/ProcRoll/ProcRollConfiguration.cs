namespace ProcRoll
{
    public class ProcRollConfiguration
    {
        public Dictionary<string, ProcessStartInfo> Processes { get; set; } = new Dictionary<string, ProcessStartInfo>();
        public Dictionary<string, ProcRollEventHandlers> EventHandlers { get; set; } = new Dictionary<string, ProcRollEventHandlers>();
    }
}