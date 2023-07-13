using System.IO.Pipes;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

[assembly: InternalsVisibleTo("ProcRoll")]
[assembly: InternalsVisibleTo("ProcRoll.Host")]

namespace ProcRoll;

/// <summary>
/// Wrapper for controlling a single instance of an external process.
/// </summary>
public partial class Process : IDisposable, IAsyncDisposable
{
    private System.Diagnostics.Process? process;
    private Regex? startedRegex;
    private bool disposedValue;
    private NamedPipeServerStream controlPipe;
    private NamedPipeServerStream stdOutPipe;
    private NamedPipeServerStream stdErrPipe;
    private NamedPipeServerStream eventsPipe;
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
    /// <param name="actions"></param>
    /// <param name="args">Replacement values for argument placeholders.</param>
    /// <returns>Instance of <see cref='ProcRoll.Process'/> for started external process.</returns>
    public static async Task<Process> Start(ProcessStartInfo startInfo, ProcessActions? actions = default, params object[] args)
    {
        var process = new Process(startInfo, actions);
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
    /// <param name="actions"></param>
    public Process(ProcessStartInfo startInfo, ProcessActions? actions = default)
    {
        StartInfo = startInfo;
        ProcessActions = actions ?? new ProcessActions();
    }

    /// <summary>
    /// Instance of <see cref='ProcRoll.ProcessStartInfo'/> with start configuration for external process.
    /// </summary>
    public ProcessStartInfo StartInfo { get; }
    /// <summary>
    /// Instance of <see cref='ProcRoll.ProcessActions'/> with actions for external process.
    /// </summary>
    public ProcessActions ProcessActions { get; }

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
    /// Write text to console of running process.
    /// </summary>
    public StreamWriter StandardInput => process?.StandardInput ?? throw new InvalidOperationException("Process not started");

    internal bool IsShelled { get; set; } = false;

    /// <summary>
    /// Start the external process.
    /// </summary>
    /// <param name="args">Replacement values for argument placeholders.</param>
    public async Task Start(params object[] args)
    {
        await Task.Run(() => ProcessActions.OnStarting?.Invoke()).ConfigureAwait(false);

        var arguments = StartInfo.Arguments ?? string.Empty;
        if (args.Length > 0)
        {
            var replacements = new Queue<object>(args);
            arguments = ArgumentPlaceholder().Replace(arguments, _ => replacements.Dequeue().ToString()!);
        }

        if (StartInfo.UseShellExecute)
        {
            await StartDetached(arguments).ConfigureAwait(false);
        }
        else
        {
            await StartAttached(arguments).ConfigureAwait(false);
        }

        _ = process!.WaitForExitAsync().ContinueWith(_ =>
        {
            if (!starting.Task.IsCompleted) { starting.SetResult(); }
            if (!executing.Task.IsCompleted) { executing.SetResult(); }
        }).ConfigureAwait(false);

        await Task.Run(() => ProcessActions.OnStarted?.Invoke()).ConfigureAwait(false);
    }

    private async Task StartAttached(string arguments)
    {
        process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = StartInfo.FileName,
                Arguments = arguments
            }
        };

        foreach (var env in StartInfo.EnvironmentVariables)
        {
            process.StartInfo.EnvironmentVariables[env.Key] = env.Value;
        }

        if (StartInfo.StartedStringMatch != null)
        {
            startedRegex = new Regex(StartInfo.StartedStringMatch, RegexOptions.Compiled);
        }

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardInput = true;
        process.OutputDataReceived += Process_OutputDataReceived;
        process.ErrorDataReceived += Process_ErrorDataReceived;
        void Process_OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e) => HandleConsoleOutput(e.Data, "Out");
        void Process_ErrorDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e) => HandleConsoleOutput(e.Data, "Err");

        await Task.Run(() =>
        {
            process.Start();

        }).ConfigureAwait(false);

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (startedRegex == null)
            starting.SetResult();
    }

    private void HandleConsoleOutput(string? data, string source)
    {
        if (data == null)
            return;

        (source switch
        {
            "Out" => ProcessActions.StdOut ?? Console.Out.WriteLine,
            "Err" => ProcessActions.StdErr ?? Console.Error.WriteLine,
            _ => throw new NotImplementedException()
        }).Invoke(data);

        if (!Started && startedRegex != null && startedRegex.IsMatch(data))
            starting.SetResult();
    }

    private async Task StartDetached(string arguments)
    {
        var detachedId = Random.Shared.Next().ToString("x8");
        controlPipe = new NamedPipeServerStream(string.Concat(PIPE_PREFIX, detachedId), PipeDirection.Out);
        stdOutPipe = new NamedPipeServerStream(string.Concat(PIPE_PREFIX, detachedId, PIPE_STDOUT), PipeDirection.In);
        stdErrPipe = new NamedPipeServerStream(string.Concat(PIPE_PREFIX, detachedId, PIPE_STDERR), PipeDirection.In);
        eventsPipe = new NamedPipeServerStream(string.Concat(PIPE_PREFIX, detachedId, PIPE_EVENTS), PipeDirection.In);

        _ = Task.Run(async () =>
        {
            await stdOutPipe.WaitForConnectionAsync().ConfigureAwait(false);
            using var sr = new StreamReader(stdOutPipe);
            string? msg;
            while ((msg = await sr.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                HandleConsoleOutput(msg, "Out");
            }
        }).ConfigureAwait(false);
        
        _ = Task.Run(async () =>
        {
            await stdErrPipe.WaitForConnectionAsync().ConfigureAwait(false);
            using var sr = new StreamReader(stdErrPipe);
            string? msg;
            while ((msg = await sr.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                HandleConsoleOutput(msg, "Err");
            }
        }).ConfigureAwait(false);

        _ = Task.Run(async () =>
        {
            await eventsPipe.WaitForConnectionAsync().ConfigureAwait(false);
            using var sr = new StreamReader(eventsPipe);
            string? msg;
            while ((msg = await sr.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                switch (msg)
                {
                    case EVENT_STARTED:
                        starting.SetResult();
                        break;
                    default:
                        break;
                }
            }
        }).ConfigureAwait(false);

        var processStartInfo = new System.Diagnostics.ProcessStartInfo();
        processStartInfo.UseShellExecute = true;

        StringBuilder hostArgs = new();
        hostArgs.Append($"Host:ID={detachedId} ");
        hostArgs.Append($"Process:FileName=\"{StartInfo.FileName}\"");
        if (!string.IsNullOrWhiteSpace(arguments))
            hostArgs.Append($" Process:Arguments=\"{arguments.Replace("\"", "\\\"")}\"");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            processStartInfo.FileName = $"{AppContext.BaseDirectory}ProcRoll.Host.exe";
            processStartInfo.Arguments = hostArgs.ToString();
        }
        else
        {
            processStartInfo.FileName = "dotnet";
            processStartInfo.Arguments = $"{AppContext.BaseDirectory}ProcRoll.Host.dll {hostArgs}";
        }

        process = new System.Diagnostics.Process { StartInfo = processStartInfo };
        process.Start();

        if (startedRegex == null)
            starting.SetResult();

        await controlPipe.WaitForConnectionAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Stop the external process.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task Stop()
    {
        if (process is null) throw new InvalidOperationException("Process not started");

        await Task.Run(() => ProcessActions.OnStopping?.Invoke()).ConfigureAwait(false);

        if (StartInfo.UseShellExecute)
        {
            using var sw = new StreamWriter(controlPipe) { AutoFlush = true };
            await sw.WriteLineAsync(CONTROL_STOP).ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);
        }
        else
        {
            switch (StartInfo.StopMethod)
            {
                case StopMethod.Default when IsShelled:
                    SendCtrlC();
                    break;
                case StopMethod.Default when !IsShelled:
                    process.Kill();
                    break;
                case StopMethod.Kill:
                    process.Kill();
                    break;
                case StopMethod.Close:
                    process.CloseMainWindow();
                    break;
                case StopMethod.CtrlC:
                    SendCtrlC();
                    break;
                case StopMethod.CtrlBreak:
                    SendCtrlBreak();
                    break;
            }
        }
        await Task.WhenAny(Executing, Task.Delay(5000));
        process.Kill();

        await Task.Run(() => ProcessActions.OnStopped?.Invoke()).ConfigureAwait(false);
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
        SetConsoleCtrlHandler(null, true);
        GenerateConsoleCtrlEvent(key, 0);
        SetConsoleCtrlHandler(null, false);
    }

    [GeneratedRegex("\\{\\w+\\}")]
    private static partial Regex ArgumentPlaceholder();

    const uint CTRL_C_EVENT = 0;
    const uint CTRL_BREAK_EVENT = 1;
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetConsoleCtrlHandler(ConsoleCtrlDelegate? HandlerRoutine, [MarshalAs(UnmanagedType.Bool)] bool Add);
    internal delegate Boolean ConsoleCtrlDelegate(uint CtrlType);

    /// <summary>
    /// Clean up resources.
    /// </summary>
    /// <param name="disposing">Flag to indicate if called from finalizer or Dispose.</param>
    protected async Task Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                if (Started && !Stopped)
                {
                    await Stop();
                }
                if (controlPipe != null)
                    await controlPipe.DisposeAsync().ConfigureAwait(false);
                if (stdOutPipe != null)
                    await stdOutPipe.DisposeAsync().ConfigureAwait(false);
                if (stdErrPipe != null)
                    await stdErrPipe.DisposeAsync().ConfigureAwait(false);
                if (eventsPipe != null)
                    await eventsPipe.DisposeAsync().ConfigureAwait(false);
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
        Dispose(disposing: true).RunSynchronously();
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

    internal const string PIPE_PREFIX = "ProcRoll:";
    internal const string PIPE_STDOUT = ":StdOut";
    internal const string PIPE_STDERR = ":StdErr";
    internal const string PIPE_EVENTS = ":Events";
    internal const string CONTROL_STOP = "STOP";
    internal const string EVENT_STARTED = "STARTED";
}