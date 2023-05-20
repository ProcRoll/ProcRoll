using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProcRoll
{
    public static class ProcRollExtensions
    {
        public static IServiceCollection AddProcRoll(this IServiceCollection services) => AddProcRoll(services, null);

        public static IServiceCollection AddProcRoll(this IServiceCollection services, Action<ProcRollConfiguration>? ProcRollConfiguration)
        {
            services.AddOptions<ProcRollConfiguration>().BindConfiguration("ProcRoll");
            services.AddSingleton<IProcRollFactory, ProcRollFactory>();

            using var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var ProcRollConfigurationInstance = configuration.GetSection("ProcRoll")?.Get<ProcRollConfiguration>() ?? new ProcRollConfiguration();
            services.AddSingleton(ProcRollConfigurationInstance);

            ProcRollConfiguration?.Invoke(ProcRollConfigurationInstance);

            if (ProcRollConfigurationInstance.Processes.Any(p => p.Value.StartMode == StartMode.Hosted))
                services.AddHostedService<ProcRollHostedService>();

            return services;
        }

        public static ProcRollConfiguration Add(this ProcRollConfiguration ProcRollConfiguration,
                                               string name,
                                               string fileName,
                                               string? arguments = default,
                                               StartMode startMode = default,
                                               StopMethod stopMethod = default,
                                               IEnumerable<KeyValuePair<string, string>>? environmentVariables = default,
                                               Action<string>? stdOut = default,
                                               Action<string>? stdErr = default,
                                               string? startMessageMatch = default,
                                               IEnumerable<string>? dependsOn = default,
                                               Action<Process>? stopping = default)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                StartMode = startMode,
                StopMethod = stopMethod,
                StdOut = stdOut,
                StdErr = stdErr,
                StartedStringMatch = startMessageMatch,
            };
            if (environmentVariables != null)
                startInfo.EnvironmentVariables = new Dictionary<string, string>(environmentVariables);
            if (dependsOn != null)
                startInfo.DependsOn.AddRange(dependsOn);

            ProcRollEventHandlers? eventHandlers;
            if (stopping != null)
            {
                eventHandlers = new() { Stopping = stopping };
                ProcRollConfiguration.EventHandlers[name] = eventHandlers;
            }

            ProcRollConfiguration.Processes[name] = startInfo;
            return ProcRollConfiguration;
        }

        public static ProcRollConfiguration WithEventHandlers(this ProcRollConfiguration ProcRollConfiguration, string name, Action<ProcRollEventHandlers> handlersConfig)
        {
            var handlers = ProcRollConfiguration.EventHandlers.TryGetValue(name, out var val) ? val : ProcRollConfiguration.EventHandlers[name] = new();
            handlersConfig(handlers);

            return ProcRollConfiguration;
        }
    }
}