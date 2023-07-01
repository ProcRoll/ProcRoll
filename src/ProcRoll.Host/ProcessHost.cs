using Microsoft.Extensions.Options;
using System.Diagnostics;
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
        if (processStartInfo.StopMethod == StopMethod.Default)
            processStartInfo.StopMethod = StopMethod.CtrlC;

        process = new Process(processStartInfo);
        await process.Start();
        //process.OnStopped = hostApplicationLifetime.StopApplication();

        stoppingToken.Register(() => process.Stop().Wait());

        var controlHandle = hostConfig.ID ?? throw new ArgumentException("'Control' missing from arguments.");
        using var controlPipe = new AnonymousPipeClientStream(controlHandle);
        using var sr = new StreamReader(controlPipe);
        string? command;
        while ((command = await sr.ReadLineAsync(stoppingToken)) != null)
        {
            switch (command)
            {
                case "Stop":
                    hostApplicationLifetime.StopApplication();
                    break;
                default:
                    await Console.Out.WriteLineAsync(command);
                    break;
            }
        }
    }
}
