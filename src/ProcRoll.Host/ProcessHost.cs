using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Pipes;

namespace ProcRoll;

public class ProcessHost : BackgroundService
{
    private readonly ILogger<ProcessHost> logger;
    private readonly IOptions<HostConfig> hostConfigOptions;
    private readonly IOptions<ProcessStartInfo> processStartInfoOptions;
    private readonly IHostApplicationLifetime hostApplicationLifetime;
    private readonly HostConfig hostConfig;
    private readonly ProcessStartInfo processStartInfo;
    private Process? process;

    public ProcessHost(ILogger<ProcessHost> logger, IOptions<HostConfig> hostConfigOptions, IOptions<ProcessStartInfo> processStartInfoOptions, IHostApplicationLifetime hostApplicationLifetime)
    {
        this.logger = logger;
        this.hostConfigOptions = hostConfigOptions;
        this.processStartInfoOptions = processStartInfoOptions;
        this.hostApplicationLifetime = hostApplicationLifetime;
        hostConfig = hostConfigOptions.Value;
        processStartInfo = processStartInfoOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting process: {filename} {arguments}", processStartInfo.FileName, processStartInfo.Arguments);

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
            StdOut = swOut.WriteLine,
            StdErr = swErr.WriteLine
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
}
