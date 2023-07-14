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
        /// <param name="configure">Delegate for configuring ProcRoll settings.</param>
        /// <returns>IHostBuilder</returns>
        public static IHostBuilder ConfigureProcRoll(this IHostBuilder host, Action<ProcRollBuilder> configure)
        {
            var actions = new Dictionary<string, Func<IServiceProvider, ProcessActions>>();
            host.ConfigureServices(services => services.AddSingleton(actions));

            var builder = new ProcRollBuilder(host, actions);
            configure(builder);
            return ConfigureServices(host, actions);
        }

        /// <summary>
        /// Enable ProcRoll. Configuration is read from "ProcRoll" section.
        /// </summary>
        /// <param name="host">IHostBuilder</param>
        /// <returns>IHostBuilder</returns>
        public static IHostBuilder ConfigureProcRoll(this IHostBuilder host) => ConfigureServices(host, new Dictionary<string, Func<IServiceProvider, ProcessActions>>());

        private static IHostBuilder ConfigureServices(IHostBuilder host, Dictionary<string, Func<IServiceProvider, ProcessActions>> actions) => 
            host.ConfigureServices((hostContext, services) =>
            {
                services.AddOptions<ProcRollConfiguration>().BindConfiguration("ProcRoll");
                services.AddSingleton<IProcRollFactory, ProcRollFactory>();
                services.AddHostedService<ProcRollHostedService>();
                services.AddSingleton(actions);
            });
    }
}