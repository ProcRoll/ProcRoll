using Microsoft.Extensions.Options;
using System.IO.Pipes;

namespace ProcRoll;

public class ProcessHost : BackgroundService
{
    private readonly IOptions<HostConfig> hostConfigOptions;
    private readonly IOptions<ProcessStartInfo> processStartInfoOptions;
    private readonly IHostApplicationLifetime hostApplicationLifetime;
    private readonly HostConfig hostConfig;
    private readonly ProcessStartInfo processStartInfo;
    private Process? process;

    public ProcessHost(IOptions<HostConfig> hostConfigOptions, IOptions<ProcessStartInfo> processStartInfoOptions, IHostApplicationLifetime hostApplicationLifetime)
    {
        this.hostConfigOptions = hostConfigOptions;
        this.processStartInfoOptions = processStartInfoOptions;
        this.hostApplicationLifetime = hostApplicationLifetime;
        hostConfig = hostConfigOptions.Value;
        processStartInfo = processStartInfoOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var controlPipeName = string.Concat(Process.PIPE_PREFIX, hostConfig.ID);
        using var controlPipe = new NamedPipeClientStream(".", controlPipeName, PipeDirection.In);
        await controlPipe.ConnectAsync(stoppingToken);

        process = new Process(processStartInfo) { IsShelled = true };
        await process.Start();
        stoppingToken.Register(() => process.Stop().Wait());

        using var sr = new StreamReader(controlPipe);
        string? command;
        while ((command = await sr.ReadLineAsync(stoppingToken)) != null)
        {
            switch (command)
            {
                case ProcRoll.Process.CONTROL_STOP:
                    hostApplicationLifetime.StopApplication();
                    break;
                default:
                    break;
            }
        }
    }
}
