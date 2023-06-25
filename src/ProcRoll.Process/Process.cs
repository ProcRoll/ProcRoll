using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ProcRoll;

/// <summary>
/// Wrapper for controlling a single instance of an external process.
/// </summary>
public partial class Process : IDisposable, IAsyncDisposable
{
    private System.Diagnostics.Process? process;
    private Regex? startedRegex;
    private bool disposedValue;
    private readonly TaskCompletionSource starting = new();
    private readonly TaskCompletionSource executing = new();

    /// <summary>
    /// Start an external process.
    /// </summary>
    /// <param name="fileName">Name of the external executable file.</param>
    /// <param name="arguments">Arguments to pass to the process.</param>
    /// <returns>Instance of <see cref='ProcRoll.Process'/> for started external process.</returns>
    public static async Task<Process> Start(string fileName, string arguments = default!)
    {
        var process = new Process(new ProcessStartInfo { FileName = fileName, Arguments = arguments });
        await process.Start().ConfigureAwait(false);
        return process;
    }

    /// <summary>
    /// Start an external process.
    /// </summary>
    /// <param name="startInfo">Instance of <see cref='ProcRoll.ProcessStartInfo'/> with start configuration for external process.</param>
    /// <param name="args">Replacement values for argument placeholders.</param>
    /// <returns>Instance of <see cref='ProcRoll.Process'/> for started external process.</returns>
    public static async Task<Process> Start(ProcessStartInfo startInfo, params object[] args)
    {
        var process = new Process(startInfo);
        await process.Start(args).ConfigureAwait(false);
        return process;
    }

    /// <summary>
    /// Initialize an instance of <see cref='ProcRoll.Process'/> for an external executable.
    /// </summary>
    /// <param name="fileName">Name of the external executable file.</param>
    /// <param name="arguments">Arguments to pass to the process. 
    /// Can include placeholders using braces <c>{}</c> for input of values when starting the process. </param>
    public Process(string fileName, string arguments = default!)
        : this(new ProcessStartInfo { FileName = fileName, Arguments = arguments }) { }

    /// <summary>
    /// Initialize an instance of <see cref='ProcRoll.Process'/> for an external executable.
    /// </summary>
    /// <param name="startInfo">Instance of <see cref='ProcRoll.ProcessStartInfo'/> with start configuration for external process.</param>
    public Process(ProcessStartInfo startInfo)
    {
        StartInfo = startInfo;
    }

    /// <summary>
    /// Instance of <see cref='ProcRoll.ProcessStartInfo'/> with start configuration for external process.
    /// </summary>
    public ProcessStartInfo StartInfo { get; }
    /// <summary>
    /// <see cref='System.Threading.Tasks.Task'/> for awaiting while process is starting.
    /// </summary>
    public Task Starting => starting.Task;
    /// <summary>
    /// <see cref='System.Threading.Tasks.Task'/> for awaiting while process is executing.
    /// </summary>
    public Task Executing => executing.Task;
    /// <summary>
    /// Process has started.
    /// </summary>
    public bool Started => starting.Task.IsCompleted;
    /// <summary>
    /// Process has stopped.
    /// </summary>
    public bool Stopped => executing.Task.IsCompleted;
    /// <summary>
    /// Action executed before starting.
    /// </summary>
    public virtual Task OnStarting() => Task.CompletedTask;
    /// <summary>
    /// Action executed after starting.
    /// </summary>
    public virtual Task OnStarted() => Task.CompletedTask;
    /// <summary>
    /// Action executed before stopping.
    /// </summary>
    public virtual Task OnStopping() => Task.CompletedTask;
    /// <summary>
    /// Action executed after stopping.
    /// </summary>
    public virtual Task OnStopped() => Task.CompletedTask;
    /// <summary>
    /// Write text to console of running process.
    /// </summary>
    public StreamWriter StandardInput => process?.StandardInput ?? throw new InvalidOperationException("Process not started");
    /// <summary>
    /// ID of the underlying process.
    /// </summary>
    public int ProcessID => process?.Id ?? throw new InvalidOperationException("Process not started.");

    /// <summary>
    /// Start the external process.
    /// </summary>
    /// <param name="args">Replacement values for argument placeholders.</param>
    public virtual async Task Start(params object[] args)
    {
        await OnStarting().ConfigureAwait(false);

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
        }).ConfigureAwait(false);

        await OnStarted().ConfigureAwait(false);
    }

    private void Process_OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            StdOut(e.Data);
            if (startedRegex != null && !Started)
                if (startedRegex.IsMatch(e.Data))
                    starting.SetResult();
        }
    }

    private void Process_ErrorDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
    {
        if (e.Data != null)
            StdErr(e.Data);
    }

    /// <summary>
    /// Receives standard output data from console process.
    /// </summary>
    /// <param name="data"></param>
    public virtual void StdOut(string data) => StartInfo.StdOut?.Invoke(data);

    /// <summary>
    /// Receives standard error data from console process.
    /// </summary>
    /// <param name="data"></param>
    public virtual void StdErr(string data) => (StartInfo.StdErr ?? StartInfo.StdOut)?.Invoke(data);

    /// <summary>
    /// Stop the external process.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public virtual async Task Stop()
    {
        await OnStopping().ConfigureAwait(false);

        if (process is null) throw new InvalidOperationException("Process not started");
        switch (StartInfo.StopMethod)
        {
            case StopMethod.CtrlC:
                SendCtrlC();
                break;
            case StopMethod.CtrlBreak:
                SendCtrlBreak();
                break;
            default:
                if (!process.CloseMainWindow())
                    process.Kill(true);
                break;
        }
        await Task.WhenAny(Executing, Task.Delay(5000));
        process.Kill(true);

        await OnStopped().ConfigureAwait(false);
    }

    /// <summary>
    /// Send <c>Ctrl+C</c> to standard input for external process.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void SendCtrlC() => SendCtrlKey(CTRL_C_EVENT);

    /// <summary>
    /// Send <c>Ctrl+Break</c> to standard input for external process.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void SendCtrlBreak() => SendCtrlKey(CTRL_BREAK_EVENT);

    void SendCtrlKey(uint key)
    {
        if (process is null) throw new InvalidOperationException("Process not started");
        if (!SetConsoleCtrlHandler(null, true)) throw new InvalidOperationException(FormatPInvokeMessage("SetConsoleCtrlHandler(null, true)"));
        if (!GenerateConsoleCtrlEvent(key, 0)) throw new InvalidOperationException(FormatPInvokeMessage("GenerateConsoleCtrlEvent(key, 0)"));
        if (!SetConsoleCtrlHandler(null, false)) throw new InvalidOperationException(FormatPInvokeMessage("SetConsoleCtrlHandler(null, false)"));

        static string FormatPInvokeMessage(string message) => $"{message} error ({Marshal.GetLastPInvokeError()}) {Marshal.GetLastPInvokeErrorMessage()}";
    }

    [GeneratedRegex("\\{\\w+\\}")]
    private static partial Regex ArgumentPlaceholder();

    const uint CTRL_C_EVENT = 0;
    const uint CTRL_BREAK_EVENT = 1;
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
    const uint ATTACH_PARENT_PROCESS = 0x0ffffffff;
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AttachConsole(uint dwProcessId);
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool FreeConsole();
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetConsoleCtrlHandler(ConsoleCtrlDelegate? HandlerRoutine, [MarshalAs(UnmanagedType.Bool)] bool Add);
    internal delegate Boolean ConsoleCtrlDelegate(uint CtrlType);
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial uint GetConsoleProcessList(uint[] ProcessList, uint ProcessCount);
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial IntPtr GetConsoleWindow();
    const int STD_INPUT_HANDLE = -10;
    const int STD_OUTPUT_HANDLE = -11;
    const int STD_ERROR_HANDLE = -12;
    const int INVALID_HANDLE_VALUE = -1;
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial IntPtr GetStdHandle(int nStdHandle);

    /// <summary>
    /// Clean up resources.
    /// </summary>
    /// <param name="disposing">Flag to indicate if called from finalizer or Dispose.</param>
    protected virtual async Task Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                if (Started && !Stopped)
                {
                    await Stop();
                }
            }

            if (process != null && !process.HasExited)
            {
                process.Kill();
            }
            process?.Dispose();

            disposedValue = true;
        }
    }

    /// <summary>
    /// Finaliser.
    /// </summary>
    ~Process()
    {
        Dispose(disposing: false).Wait();
    }

    /// <summary>
    /// Managed dispose.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true).Wait();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Async managed dispose.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await Dispose(disposing: true).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}