using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ProcRoll
{
    public class ProcRollHostedService : BackgroundService
    {
        private readonly ILogger<ProcRollHostedService> logger;
        private readonly IProcRollFactory ProcRollFactory;

        public ProcRollHostedService(ILogger<ProcRollHostedService> logger,
                                    IProcRollFactory ProcRollFactory)
        {
            this.logger = logger;
            this.ProcRollFactory = ProcRollFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogDebug("Starting hosted ProcRoll processes");
            var hostedProcesses = ProcRollFactory.Config.Processes
                .Where(p => p.Value.StartMode == StartMode.Hosted)
                .Select(p => ProcRollFactory.Start(p.Key).Result).ToList();

            await Task.Yield();
            stoppingToken.WaitHandle.WaitOne();

            await Task.WhenAll(hostedProcesses.Select(p => ProcRollFactory.Stop(p)));
        }
    }
}