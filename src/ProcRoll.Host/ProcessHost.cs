namespace ProcRoll;

/// <summary>
/// Host for control a single external process.
/// </summary>
public class ProcessHost : BackgroundService
{
    private readonly ILogger<ProcessHost> _logger;

    /// <summary>
    /// Construct instance from dependency injection.
    /// </summary>
    /// <param name="logger">Process logger.</param>
    public ProcessHost(ILogger<ProcessHost> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Start the external process.
    /// </summary>
    /// <param name="stoppingToken">Token for receiving signal to stop.</param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
