namespace ProcRoll
{
    /// <summary>
    /// Event handlers for a process.
    /// </summary>
    public class ProcRollEventHandlers
    {
        /// <summary>
        /// Action to allow custom actions for stopping the process.
        /// </summary>
        public Action<Process>? Stopping { get; set; }

        internal void Process_Stopping(object? sender, EventArgs e)
        {
            if (sender is Process process)
            {
                Stopping?.Invoke(process);
            }
        }
    }
}
