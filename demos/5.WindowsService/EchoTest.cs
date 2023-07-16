using ProcRoll;

internal class EchoTest : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var process = Process.Start(new ProcessStartInfo 
        {
            FileName = $"{AppContext.BaseDirectory}ProcRoll.Tests.Echo.exe", 
            UseShellExecute = true 
        });
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
