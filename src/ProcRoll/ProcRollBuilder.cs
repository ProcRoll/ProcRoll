using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ProcRoll
{
    /// <summary>
    /// Builder for hosted ProcRoll processing.
    /// </summary>
    public class ProcRollBuilder
    {
        private readonly IHostBuilder host;
        private readonly Dictionary<string, Func<IServiceProvider, ProcessActions>> actions;

        /// <summary>
        /// Constructor used by ConfiureProcRoll() extension method.
        /// </summary>
        /// <param name="host">Instance of <see cref="IHostBuilder"/></param>
        /// <param name="actions">Instance of Dictionary{string, Func{IServiceProvider, ProcessActions}}</param>
        public ProcRollBuilder(IHostBuilder host, Dictionary<string, Func<IServiceProvider, ProcessActions>> actions)
        {
            this.host = host;
            this.actions = actions;
        }

        /// <summary>
        /// Add a process definition that uses an event handler class. The instance of the handler class will be created using dependency injection.
        /// </summary>
        /// <param name="name">Name of the definition.</param>
        /// <param name="fileName">Name of the process file.</param>
        /// <param name="arguments">Arguments to be passed to the process. Can contain placeholders for substition when starting.</param>
        /// <param name="startMode">How the process will be started.</param>
        /// <param name="stopMethod">How the process will be stopped.</param>
        /// <param name="environmentVariables">A dictionary of environment variables to be set for the running process.</param>
        /// <param name="startedStringMatch">A Regex query to identify a console message to indicate a process has fully started.</param>
        /// <param name="dependsOn">Method needed to stop the external process.</param>
        /// <param name="useShellExecute">Use ProcRoll.ProcessHost to run process./></param>
        /// <param name="processActions">Instance of <see cref="ProcessActions"/> for configuring event handlers for processes.</param>
        /// <returns></returns>
        public ProcRollBuilder Add(string name,
                                   string fileName,
                                   string? arguments = null,
                                   StartMode startMode = StartMode.Default,
                                   StopMethod stopMethod = StopMethod.Default,
                                   IEnumerable<KeyValuePair<string, string>>? environmentVariables = null,
                                   string? startedStringMatch = null,
                                   IEnumerable<string>? dependsOn = null,
                                   bool useShellExecute = false,
                                   Func<IServiceProvider, ProcessActions>? processActions = null)
        {
            return Add(name, new HostedStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                StartMode = startMode,
                StopMethod = stopMethod,
                EnvironmentVariables = environmentVariables?.ToDictionary(d => d.Key, d => d.Value)!,
                StartedStringMatch = startedStringMatch,
                DependsOn = dependsOn?.ToList()!,
                UseShellExecute = useShellExecute
            }, processActions);
        }

        /// <summary>
        /// Add a process definition.
        /// </summary>
        /// <param name="name">Name of the definition.</param>
        /// <param name="hostedStartInfo">The settings for the process.</param>
        /// <param name="processActions">Instance of <see cref="ProcessActions"/> for configuring event handlers for processes.</param>
        /// <returns></returns>
        public ProcRollBuilder Add(string name, HostedStartInfo hostedStartInfo, Func<IServiceProvider, ProcessActions>? processActions = null)
        {
            host.ConfigureAppConfiguration(configBuilder =>
            {
                var config = new Dictionary<string, string?>
                {
                    [$"ProcRoll:Processes:{name}:FileName"] = hostedStartInfo.FileName,
                    [$"ProcRoll:Processes:{name}:Arguments"] = hostedStartInfo.Arguments,
                    [$"ProcRoll:Processes:{name}:StartMode"] = hostedStartInfo.StartMode.ToString(),
                    [$"ProcRoll:Processes:{name}:StopMethod"] = hostedStartInfo.StopMethod.ToString(),
                    [$"ProcRoll:Processes:{name}:StartedStringMatch"] = hostedStartInfo.StartedStringMatch,
                    [$"ProcRoll:Processes:{name}:UseShellExecute"] = hostedStartInfo.UseShellExecute.ToString(),
                };
                if (hostedStartInfo.EnvironmentVariables != null)
                {
                    foreach (var variable in hostedStartInfo.EnvironmentVariables)
                    {
                        config[$"ProcRoll:Processes:{name}:EnvironmentVariables:{variable.Key}"] = variable.Value;
                    }
                }
                if (hostedStartInfo.DependsOn != null)
                {
                    int pos = 0;
                    foreach (var depend in hostedStartInfo.DependsOn)
                    {
                        config[$"ProcRoll:Processes:{name}:DependsOn:{pos++}"] = depend;
                    }
                }
                configBuilder.AddInMemoryCollection(config);
                var added = configBuilder.Sources.Last();
                configBuilder.Sources.Remove(added);
                configBuilder.Sources.Insert(0, added);

                if (processActions != null)
                {
                    actions[name] = processActions;
                }
            });
            return this;
        }
    }
}
