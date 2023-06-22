using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Net.Http;

namespace ProcRoll
{
    /// <summary>
    /// 
    /// </summary>
    public class ProcRollBuilder
    {
        private readonly IHostBuilder host;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="host"></param>
        public ProcRollBuilder(IHostBuilder host)
        {
            this.host = host;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="fileName"></param>
        /// <param name="arguments"></param>
        /// <param name="startMode"></param>
        /// <param name="stopMethod"></param>
        /// <param name="environmentVariables"></param>
        /// <param name="startedStringMatch"></param>
        /// <param name="dependsOn"></param>
        public void Add<T>(string name, string fileName, string? arguments = null, StartMode startMode = StartMode.Default, StopMethod stopMethod = StopMethod.Default, IEnumerable<KeyValuePair<string, string>>? environmentVariables = null, string? startedStringMatch = null, IEnumerable<string>? dependsOn = null)
        {
            Add(name, fileName, arguments, startMode, stopMethod, environmentVariables?.ToDictionary(d => d.Key, d => d.Value)!, startedStringMatch, dependsOn?.ToList()!, typeof(T));
        }

        public void Add(string name, string fileName, string? arguments = null, StartMode startMode = StartMode.Default, StopMethod stopMethod = StopMethod.Default, IEnumerable<KeyValuePair<string, string>>? environmentVariables = null, string? startedStringMatch = null, IEnumerable<string>? dependsOn = null, Type handler = null!)
        {
            Add(name, new HostedStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                StartMode = startMode,
                StopMethod = stopMethod,
                EnvironmentVariables = environmentVariables?.ToDictionary(d => d.Key, d => d.Value)!,
                StartedStringMatch = startedStringMatch,
                DependsOn = dependsOn?.ToList()!,
                Handler = handler.AssemblyQualifiedName
            });
        }

        public void Add(string name, HostedStartInfo hostedStartInfo)
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
                    [$"ProcRoll:Processes:{name}:Handler"] = hostedStartInfo.Handler
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
            });
        }
    }
}
