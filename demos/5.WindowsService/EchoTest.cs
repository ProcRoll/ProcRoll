using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class EchoTest : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var process = Process.Start($"{AppContext.BaseDirectory}Echo\\ProcRoll.Tests.Echo.exe");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
