using System.Xml.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ProcRoll
{
    /// <summary>
    /// Uses <see cref="Microsoft.Extensions.Hosting.BackgroundService"/> to run external process.
    /// </summary>
    public class ProcRollHostedService : BackgroundService
    {
        private readonly ILogger<ProcRollHostedService> logger;
        private readonly IProcRollFactory ProcRollFactory;

        /// <summary>
        /// Create instance of <see cref="ProcRoll.ProcRollHostedService"/> from dependency injection.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="ProcRollFactory"></param>
        public ProcRollHostedService(ILogger<ProcRollHostedService> logger, IProcRollFactory ProcRollFactory)
        {
            this.logger = logger;
            this.ProcRollFactory = ProcRollFactory;
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="stoppingToken">Indicates that the start process has been aborted.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogDebug("Starting hosted ProcRoll processes");

            var hostedProcesses = ProcRollFactory.Config.Processes
                .Where(p => p.Value.StartMode == StartMode.Hosted)
                .ToDictionary(p => p.Key, p => ProcRollFactory.Start(p.Key).Result);

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException) { }

            logger.LogDebug("Stopping hosted ProcRoll processes");

            await Task.WhenAll(hostedProcesses.AsParallel().Select(p => ProcRollFactory.Stop(p.Key, p.Value)).ToArray());

            logger.LogDebug("Stopped hosted ProcRoll processes");
        }
    }
}