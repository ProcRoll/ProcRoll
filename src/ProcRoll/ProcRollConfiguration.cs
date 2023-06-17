namespace ProcRoll
{
    /// <summary>
    /// Repository for all configurations in hosted application.
    /// </summary>
    public class ProcRollConfiguration
    {
        /// <summary>
        /// The configured process start configurations.
        /// </summary>
        public Dictionary<string, ProcessStartInfo> Processes { get; set; } = new Dictionary<string, ProcessStartInfo>();
    }
}