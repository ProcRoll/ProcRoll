using ProcRoll;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging => logging
        .SetMinimumLevel(LogLevel.Trace)
        .AddFilter((source, level) => source is null || !source.StartsWith("Microsoft") || level > LogLevel.Information)
        .AddSimpleConsole(configure => configure.SingleLine = true))
    .ConfigureProcRoll(configureProcRoll => configureProcRoll
        .Add("Echo1",
             "dotnet",
             $"{AppContext.BaseDirectory}ProcRoll.Tests.Echo.dll \"Success 1\" --repeat",
             StartMode.Background,
             StopMethod.CtrlC,
             startedStringMatch: "Success",
             useShellExecute: true,
             processActions: (services) =>
             {
                 var logger = services.GetService<ILoggerFactory>()!.CreateLogger("ProcRoll.Echo1");
                 return new ProcessActions
                 {
                     StdOut = (msg) => logger.LogInformation("1> {msg}", msg),
                     StdErr = (msg) => logger.LogWarning("1> {msg}", msg),
                     OnStarting = () => logger.LogTrace("1> OnStarting 1"),
                     OnStarted = () => logger.LogTrace("1> OnStarted 1"),
                     OnStopping = () => logger.LogTrace("1> OnStopping 1"),
                     OnStopped = () => logger.LogTrace("1> OnStopped 1")
                 };
             })
        .Add("Echo2",
             "dotnet",
             $"{AppContext.BaseDirectory}ProcRoll.Tests.Echo.dll \"Success 2\" --repeat",
             StartMode.Hosted,
             StopMethod.CtrlC,
             dependsOn: new[] { "Echo1" },
             useShellExecute: true,
             processActions: (services) =>
             {
                 var logger = services.GetService<ILoggerFactory>()!.CreateLogger("ProcRoll.Echo2");
                 return new ProcessActions
                 {
                     StdOut = (msg) => logger.LogInformation("2> {msg}", msg),
                     StdErr = (msg) => logger.LogWarning("2> {msg}", msg),
                     OnStarting = () => logger.LogTrace("2> OnStarting 2"),
                     OnStarted = () => logger.LogTrace("2> OnStarted 2"),
                     OnStopping = () => logger.LogTrace("2> OnStopping 2"),
                     OnStopped = () => logger.LogTrace("2> OnStopped 2")
                 };
             }))
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var stopTokenSource = new CancellationTokenSource();
var hostTask = host.RunAsync(stopTokenSource.Token);

logger.LogInformation("Press any key to stop...");
Console.ReadKey(true);
stopTokenSource.Cancel();
await host.WaitForShutdownAsync();
logger.LogInformation("Stopped");
