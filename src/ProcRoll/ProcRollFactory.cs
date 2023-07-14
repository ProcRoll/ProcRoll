using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System;
using System.Xml.Linq;

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
        /// <param name="name"></param>
        /// <param name="process"></param>
        /// <returns></returns>
        Task Stop(string name, Process process);
    }

    /// <summary>
    /// Service class for controlling hosted process.
    /// </summary>
    public partial class ProcRollFactory : IProcRollFactory
    {
        private readonly ILogger<ProcRollFactory> logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly IServiceProvider serviceProvider;
        private readonly IOptions<ProcRollConfiguration> config;
        private readonly Dictionary<string, Func<IServiceProvider, ProcessActions>> actions;
        private readonly Dictionary<string, LinkedList<Process>> startedProcesses = new();
        private readonly List<(Process Parent, string Dependency, Process Child)> processesDependencies = new();

        /// <summary>
        /// Creates an instance <see cref="ProcRoll.ProcRollFactory"/> with injected dependencies.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="config"></param>
        /// <param name="actions"></param>
        public ProcRollFactory(ILogger<ProcRollFactory> logger, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IOptions<ProcRollConfiguration> config, Dictionary<string, Func<IServiceProvider, ProcessActions>> actions)
        {
            this.logger = logger;
            this.loggerFactory = loggerFactory;
            this.serviceProvider = serviceProvider;
            this.config = config;
            this.actions = actions;
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
                    logger.LogDebug("Running instance of ProcRoll '{name}' found", name);
                    return running;
                }
            }

            logger.LogDebug("Starting ProcRoll '{name}'", name);

            var processActions = actions.ContainsKey(name) ? actions[name](serviceProvider) : new ProcessActions();
            var procLogger = loggerFactory.CreateLogger($"ProcRoll.{name}");
            if (processActions.StdOut == null)
                processActions.StdOut = (msg) => procLogger.LogInformation("{msg}", msg);
            if (processActions.StdErr == null)
                processActions.StdErr = (msg) => procLogger.LogWarning("{msg}", msg);

            var process = new Process(startInfo, processActions);

            processes.AddLast(process);

            foreach (var dependency in startInfo.DependsOn)
            {
                logger.LogDebug("Resolving dependency '{dependency}' for ProcRoll '{name}'", dependency, name);

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
                        processesDependencies.Add((process, dependency, lastDep));
                        if (!lastDep.Started)
                            await lastDep.Starting;
                    }
                    else
                    {
                        var newDep = await Start(dependency);
                        processesDependencies.Add((process, dependency, newDep));
                        await newDep.Starting;
                    }
                }
            }

            await process.Start(args);

            logger.LogDebug("Started ProcRoll '{name}'", name);

            return process;
        }

        /// <summary>
        /// Stop a process instance.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="process">The process to stop.</param>
        public async Task Stop(string name, Process process)
        {
            if (process.Stopped) return;

            logger.LogDebug("Stopping ProcRoll '{name}'", name);

            await Task.WhenAll(processesDependencies.Where(d => d.Parent == process).Select(d =>
            {
                logger.LogDebug("Stopping dependency ProcRoll '{dependency}'", d.Dependency);
                return Stop(d.Dependency, d.Child);
            }));
            await process.Stop();

            logger.LogDebug("Stopped ProcRoll '{name}'", name);
        }
    }
}