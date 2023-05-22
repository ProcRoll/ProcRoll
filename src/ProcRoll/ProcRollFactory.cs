using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ProcRoll
{
    public interface IProcRollFactory
    {
        ProcRollConfiguration Config { get; }
        Task<Process> Start(string name, params object[] args);
        Task Stop(Process process);
    }

    public partial class ProcRollFactory : IProcRollFactory
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly Dictionary<string, LinkedList<Process>> startedProcesses = new();
        private readonly List<(Process Parent, Process Child)> processesDependencies = new();

        public ProcRollFactory(ILoggerFactory loggerFactory, ProcRollConfiguration config, IConfiguration testConfig)
        {
            this.loggerFactory = loggerFactory;
            Config = config;
            Console.WriteLine(testConfig["test"]);
        }

        public ProcRollConfiguration Config { get; }

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
            startInfo.StdOut = message => logger.LogInformation("{message}", message);
            startInfo.StdErr = message => logger.LogWarning("{message}", message);

            var process = new Process(startInfo);
            if (Config.EventHandlers.TryGetValue(name, out var handlers))
            {
                if (handlers.Stopping != null)
                {
                    process.Stopping += handlers.Process_Stopping;
                }
            }
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

            process.Start(args);
            return process;
        }

        public async Task Stop(Process process)
        {
            if (process.Stopped) return;
            await Task.WhenAll(processesDependencies.Where(d => d.Parent == process).Select(d => Stop(d.Child)));
            await process.Stop();
        }
    }
}