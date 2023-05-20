namespace ProcRoll
{
    public class ProcRollEventHandlers
    {
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
