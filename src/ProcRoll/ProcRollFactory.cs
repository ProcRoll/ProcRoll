using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ProcRoll
{
    /// <summary>
    /// Injectable interface for <see cref="ProcRoll.ProcRollFactory"/>
    /// </summary>
    public interface IProcRollFactory
    {
        /// <summary>
        /// All ProcRoll configurations.
        /// </summary>
        ProcRollConfiguration Config { get; }
        /// <summary>
        /// Start a process using a named configuration.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns>Instance of <see cref="ProcRoll.Process"/> for started process.</returns>
        Task<Process> Start(string name, params object[] args);
        /// <summary>
        /// Stop a process instance.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        Task Stop(Process process);
    }

    /// <summary>
    /// Service class for controlling hosted process.
    /// </summary>
    public partial class ProcRollFactory : IProcRollFactory
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly IServiceProvider serviceProvider;
        private readonly IOptions<ProcRollConfiguration> config;
        private readonly Dictionary<string, LinkedList<Process>> startedProcesses = new();
        private readonly List<(Process Parent, Process Child)> processesDependencies = new();

        /// <summary>
        /// Creates an instance <see cref="ProcRoll.ProcRollFactory"/> with injected dependencies.
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="config"></param>
        public ProcRollFactory(ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IOptions<ProcRollConfiguration> config)
        {
            this.loggerFactory = loggerFactory;
            this.serviceProvider = serviceProvider;
            this.config = config;
        }

        /// <summary>
        /// All ProcRoll configurations.
        /// </summary>
        public ProcRollConfiguration Config => config.Value;

        /// <summary>
        /// Start a process using a named configuration.
        /// </summary>
        /// <param name="name">Name of the process configuration.</param>
        /// <param name="args">Optional values to substitue argument placeholders with.</param>
        /// <returns>Instance of <see cref="ProcRoll.Process"/> for started process.</returns>
        public async Task<Process> Start(string name, params object[] args)
        {
            var startInfo = Config.Processes[name];
            var processes = startedProcesses.TryGetValue(name, out var value) ? value : startedProcesses[name] = new();

            if (startInfo.StartMode is StartMode.Background or StartMode.Hosted)
            {
                var running = processes.FirstOrDefault(p => !p.Stopped);
                if (running != null)
                {
                    return running;
                }
            }

            var logger = loggerFactory.CreateLogger($"ProcRoll.{name}");
            var actions = new ProcessActions
            {
                StdOut = message => logger.LogInformation("{message}", message),
                StdErr = message => logger.LogWarning("{message}", message)
            };

            var process = new Process(startInfo);

            processes.AddLast(process);

            foreach (var dependency in startInfo.DependsOn)
            {
                var depProcesses = startedProcesses.TryGetValue(dependency, out var depProcessesValue) ? depProcessesValue : new();

                if (Config.Processes[dependency].StartMode == StartMode.Default)
                {
                    await (await Start(dependency)).Executing;
                }
                else
                {
                    var lastDep = depProcesses.Where(p => !p.Stopped).LastOrDefault();
                    if (lastDep != null)
                    {
                        processesDependencies.Add((process, lastDep));
                        if (!lastDep.Started)
                            await lastDep.Starting;
                    }
                    else
                    {
                        var newDep = await Start(dependency);
                        processesDependencies.Add((process, newDep));
                        await newDep.Starting;
                    }
                }
            }

            await process.Start(args);
            return process;
        }

        /// <summary>
        /// Stop a process instance.
        /// </summary>
        /// <param name="process">The process to stop.</param>
        public async Task Stop(Process process)
        {
            if (process.Stopped) return;
            await Task.WhenAll(processesDependencies.Where(d => d.Parent == process).Select(d => Stop(d.Child)));
            await process.Stop();
        }
    }
}