using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ProcRoll
{
    /// <summary>
    /// Extension methods for integrating ProcRoll with Hosting frameworks.
    /// </summary>
    public static class ProcRollExtensions
    {
        /// <summary>
        /// Enable ProcRoll. Configuration is read from "ProcRoll" section.
        /// </summary>
        /// <param name="host">IHostBuilder</param>
        /// <param name="configureProcRoll">Delegate for configuring ProcRoll settings.</param>
        /// <returns>IHostBuilder</returns>
        public static IHostBuilder ConfigureProcRoll(this IHostBuilder host, Action<ProcRollBuilder> configureProcRoll)
        {
            var builder = new ProcRollBuilder(host);
            configureProcRoll(builder);
            ConfigureProcRoll(host);
            return host;
        }

        /// <summary>
        /// Enable ProcRoll. Configuration is read from "ProcRoll" section.
        /// </summary>
        /// <param name="host">IHostBuilder</param>
        /// <returns>IHostBuilder</returns>
        public static IHostBuilder ConfigureProcRoll(this IHostBuilder host)
        {
            host.ConfigureServices((hostContext, services) =>
            {
                services.AddOptions<ProcRollConfiguration>().BindConfiguration("ProcRoll");
                services.AddSingleton<IProcRollFactory, ProcRollFactory>();
                services.AddHostedService<ProcRollHostedService>();
            });
            return host;
        }
    }
}