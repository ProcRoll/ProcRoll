using Microsoft.Extensions.Options;
using System.IO.Pipes;

namespace ProcRoll;

/// <summary>
/// Service for hosting ProcRoll process and communicating with controlling service.
/// </summary>
public partial class ProcessHost : BackgroundService
{
    private readonly ILogger<ProcessHost> logger;
    private readonly IHostApplicationLifetime hostApplicationLifetime;
    private readonly HostConfig hostConfig;
    private readonly ProcessStartInfo processStartInfo;
    private Process? process;

    /// <summary>
    /// Create instance of ProcessHost from dependency injection.
    /// </summary>
    /// <param name="logger">Instance of <see cref="ILogger{ProcessHost}"/></param>
    /// <param name="hostConfigOptions">Instance of <see cref="IOptions{HostConfig}"/></param>
    /// <param name="processStartInfoOptions">Instance of <see cref="IOptions{ProcessStartInfo}"/></param>
    /// <param name="hostApplicationLifetime">Instance of <see cref="IHostApplicationLifetime"/></param>
    public ProcessHost(ILogger<ProcessHost> logger, IOptions<HostConfig> hostConfigOptions, IOptions<ProcessStartInfo> processStartInfoOptions, IHostApplicationLifetime hostApplicationLifetime)
    {
        this.logger = logger;
        this.hostApplicationLifetime = hostApplicationLifetime;
        hostConfig = hostConfigOptions.Value;
        processStartInfo = processStartInfoOptions.Value;
    }

    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// </summary>
    /// <param name="stoppingToken">Indicates that the start process has been aborted.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (processStartInfo.Arguments == null)
            LogStarting(processStartInfo.FileName);
        else
            LogStarting(processStartInfo.FileName, processStartInfo.Arguments);

        using var controlPipe = new NamedPipeClientStream(".", string.Concat(Process.PIPE_PREFIX, hostConfig.ID), PipeDirection.In);
        using var stdOutPipe = new NamedPipeClientStream(".", string.Concat(Process.PIPE_PREFIX, hostConfig.ID, Process.PIPE_STDOUT), PipeDirection.Out);
        using var stdErrPipe = new NamedPipeClientStream(".", string.Concat(Process.PIPE_PREFIX, hostConfig.ID, Process.PIPE_STDERR), PipeDirection.Out);
        using var eventsPipe = new NamedPipeClientStream(".", string.Concat(Process.PIPE_PREFIX, hostConfig.ID, Process.PIPE_EVENTS), PipeDirection.Out);
        await Task.WhenAll(
            controlPipe.ConnectAsync(stoppingToken),
            stdOutPipe.ConnectAsync(stoppingToken),
            stdErrPipe.ConnectAsync(stoppingToken),
            eventsPipe.ConnectAsync(stoppingToken)
        );
        using var swOut = new StreamWriter(stdOutPipe) { AutoFlush = true };
        using var swErr = new StreamWriter(stdErrPipe) { AutoFlush = true };
        using var swEvt = new StreamWriter(eventsPipe) { AutoFlush = true };

        var actions = new ProcessActions
        {
            StdOut = stdOutPipe.IsConnected ? swOut.WriteLine : Console.Out.WriteLine,
            StdErr = stdErrPipe.IsConnected ? swErr.WriteLine : Console.Error.WriteLine,
        };

        process = new Process(processStartInfo, actions) { IsShelled = true };

        _ = process.Starting.ContinueWith(async _ => await swEvt.WriteLineAsync(Process.EVENT_STARTED));

        await process.Start();
        stoppingToken.Register(() => process.Stop().Wait());

        using var sr = new StreamReader(controlPipe);
        string? command;
        while ((command = await sr.ReadLineAsync(stoppingToken)) != null)
        {
            switch (command)
            {
                case Process.CONTROL_STOP:
                    await Console.Out.WriteLineAsync("Process.CONTROL_STOP");
                    hostApplicationLifetime.StopApplication();
                    break;
                default:
                    break;
            }
        }


    }

    /// <summary>
    /// Log message for starting.
    /// </summary>
    /// <param name="filename">Name of the external executable file.</param>
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Starting process: {filename}")]
    public partial void LogStarting(string filename);

    /// <summary>
    /// Log message for starting.
    /// </summary>
    /// <param name="filename">Name of the external executable file.</param>
    /// <param name="arguments">Arguments to pass to the process.</param>
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Starting process: {filename} {arguments}")]
    public partial void LogStarting(string filename, string arguments);
}
