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
        /// <returns></returns>
        public static IHostBuilder ConfigureProcRoll(this IHostBuilder host, Action<ProcRollBuilder> configureProcRoll)
        {
            var builder = new ProcRollBuilder(host);
            configureProcRoll(builder);
            ConfigureProcRoll(host);
            return host;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
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