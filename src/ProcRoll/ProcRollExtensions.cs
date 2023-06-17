using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        public static IServiceCollection AddProcRoll(this IServiceCollection services) => AddProcRoll(services, null);

        /// <summary>
        /// Enable ProcRoll. Update ProcRollConfiguration with process start configurations.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="procRollConfiguration"></param>
        /// <returns></returns>
        public static IServiceCollection AddProcRoll(this IServiceCollection services, Action<ProcRollConfiguration>? procRollConfiguration)
        {
            services.AddOptions<ProcRollConfiguration>().BindConfiguration("ProcRoll");
            services.AddSingleton<IProcRollFactory, ProcRollFactory>();

            using var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var procRollConfigurationInstance = configuration.GetSection("ProcRoll")?.Get<ProcRollConfiguration>() ?? new ProcRollConfiguration();
            services.AddSingleton(procRollConfigurationInstance);

            procRollConfiguration?.Invoke(procRollConfigurationInstance);

            if (procRollConfigurationInstance.Processes.Any(p => p.Value.StartMode == StartMode.Hosted))
                services.AddHostedService<ProcRollHostedService>();

            return services;
        }

        /// <summary>
        /// Add a process configuration to the ProcRollConfiguration service.
        /// </summary>
        /// <param name="procRollConfiguration"></param>
        /// <param name="name"></param>
        /// <param name="fileName"></param>
        /// <param name="arguments"></param>
        /// <param name="startMode"></param>
        /// <param name="stopMethod"></param>
        /// <param name="environmentVariables"></param>
        /// <param name="stdOut"></param>
        /// <param name="stdErr"></param>
        /// <param name="startedStringMatch"></param>
        /// <param name="dependsOn"></param>
        /// <param name="starting"></param>
        /// <param name="started"></param>
        /// <param name="stopping"></param>
        /// <returns></returns>
        public static ProcRollConfiguration Add(this ProcRollConfiguration procRollConfiguration,
                                                string name,
                                                string fileName,
                                                string? arguments = default,
                                                StartMode startMode = StartMode.Default,
                                                StopMethod stopMethod = StopMethod.Default,
                                                IEnumerable<KeyValuePair<string, string>>? environmentVariables = default,
                                                Action<string>? stdOut = default,
                                                Action<string>? stdErr = default,
                                                string? startedStringMatch = default,
                                                IEnumerable<string>? dependsOn = default,
                                                Func<Process, Task>? starting = default,
                                                Func<Process, Task>? started = default,
                                                Func<Process, Task>? stopping = default)
        {
            var startInfo = procRollConfiguration.Processes.TryGetValue(name, out var value) ? value : new();

            startInfo.FileName ??= fileName;
            startInfo.Arguments ??= arguments;
            startInfo.StartMode ??= startMode;
            startInfo.StopMethod ??= stopMethod;
            startInfo.StdOut = stdOut;
            startInfo.StdErr = stdErr;
            startInfo.StartedStringMatch ??= startedStringMatch;
            startInfo.Starting ??= starting;
            startInfo.Started ??= started;
            startInfo.Stopping ??= stopping;

            if (environmentVariables != null)
            {
                foreach (var item in environmentVariables)
                {
                    startInfo.EnvironmentVariables.TryAdd(item.Key, item.Value);
                }
            }

            if (dependsOn != null)
                startInfo.DependsOn.AddRange(dependsOn);

            procRollConfiguration.Processes[name] = startInfo;
            return procRollConfiguration;
        }
    }
}