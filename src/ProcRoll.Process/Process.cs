using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ProcRoll
{
    public partial class Process
    {
        private System.Diagnostics.Process? process;
        private Regex? startedRegex;
        private readonly Dictionary<Regex, string> autoReponses;
        private readonly TaskCompletionSource starting = new();
        private readonly TaskCompletionSource executing = new();

        public static Process Start(string fileName, string arguments = default!)
        {
            var process = new Process(new ProcessStartInfo { FileName = fileName, Arguments = arguments });
            process.Start();
            return process;
        }

        public Process(string fileName, string arguments = default!)
            : this(new ProcessStartInfo { FileName = fileName, Arguments = arguments }) { }

        public Process(ProcessStartInfo startInfo)
        {
            StartInfo = startInfo;
            autoReponses = StartInfo.AutoResponses.ToDictionary(a => new Regex(a.Key, RegexOptions.Compiled), a => a.Value);
        }

        public ProcessStartInfo StartInfo { get; }
        public Task Starting => starting.Task;
        public Task Executing => executing.Task;
        public bool Started => starting.Task.IsCompleted;
        public bool Stopped => executing.Task.IsCompleted;
        public StreamWriter StandardInput => process?.StandardInput ?? throw new InvalidOperationException("Process not started");

        public void Start(params object[] args)
        {
            process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = StartInfo.FileName,
                    Arguments = StartInfo.Arguments,
                }
            };
            if (process.StartInfo.Arguments != null && args.Length > 0)
            {
                var replacements = new Queue<object>(args);
                process.StartInfo.Arguments = ArgumentPlaceholder().Replace(process.StartInfo.Arguments, _ => replacements.Dequeue().ToString()!);
            }

            foreach (var env in StartInfo.EnvironmentVariables)
            {
                process.StartInfo.EnvironmentVariables[env.Key] = env.Value;
            }
            if (StartInfo.StdOut != null)
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;
                process.OutputDataReceived += Process_OutputDataReceived;
                process.ErrorDataReceived += Process_ErrorDataReceived;
            }
            if (StartInfo.StartedStringMatch != null)
                startedRegex = new Regex(StartInfo.StartedStringMatch, RegexOptions.Compiled);

            process.Start();

            if (StartInfo.StdOut != null)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            if (startedRegex == null)
                starting.SetResult();

            _ = process.WaitForExitAsync().ContinueWith(_ =>
            {
                if (!starting.Task.IsCompleted) { starting.SetResult(); }
                if (!executing.Task.IsCompleted) { executing.SetResult(); }
            });
        }

        private void Process_OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                StartInfo.StdOut?.Invoke(e.Data);
                if (startedRegex != null && !Started)
                    if (startedRegex.IsMatch(e.Data))
                        starting.SetResult();
                var response = autoReponses.FirstOrDefault(a => a.Key.IsMatch(e.Data));
                if (response.Key != null)
                {
                    StandardInput.WriteLine(response.Value);
                }
            }
        }

        private void Process_ErrorDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            if (e.Data != null)
                (StartInfo.StdErr ?? StartInfo.StdOut)?.Invoke(e.Data);
        }

        public async Task Stop()
        {
            if (process is null) throw new InvalidOperationException("Process not started");
            switch (StartInfo.StopMethod)
            {
                case StopMethod.CtrlC:
                    SendCtrlC();
                    break;
                case StopMethod.Default when Stopping is not null:
                    Stopping.Invoke(this, EventArgs.Empty);
                    break;
                default:
                    process.Kill(true);
                    break;
            }
            await Task.WhenAny(Executing, Task.Delay(5000));
            process.Kill(true);
        }

        public event EventHandler? Stopping;

        public void SendCtrlC()
        {
            if (process is null) throw new InvalidOperationException("Process not started");
            FreeConsole();
            if (AttachConsole((uint)process.Id))
            {
                SetConsoleCtrlHandler(null, true);
                GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0);
                FreeConsole();
            }
            AttachConsole((uint)Environment.ProcessId);
        }

        [GeneratedRegex("\\{\\w+\\}")]
        private static partial Regex ArgumentPlaceholder();

        const int CTRL_C_EVENT = 0;
        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool AttachConsole(uint dwProcessId);
        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool FreeConsole();
        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetConsoleCtrlHandler(ConsoleCtrlDelegate? HandlerRoutine, [MarshalAs(UnmanagedType.Bool)] bool Add);
        internal delegate Boolean ConsoleCtrlDelegate(uint CtrlType);
    }
}