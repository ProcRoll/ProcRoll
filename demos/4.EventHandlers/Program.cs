using ProcRoll;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddProcRoll(config =>
        {
            config.Add("Echo",
                       "dotnet",
                       "ProcRoll.Tests.Echo.dll \"Success\" --repeat --usebreak",
                       StartMode.Hosted,
                       starting: async p =>
                       {
                           await Console.Out.WriteLineAsync($"Starting process");
                       },
                       started: async p =>
                       {
                           await Console.Out.WriteLineAsync($"Started process {p.ProcessID}");
                       },
                       stopping: async p =>
                       {
                           await Console.Out.WriteLineAsync($"Stopping process {p.ProcessID}");
                           p.SendCtrlBreak();
                       });
        });
    })
    .Build();

host.Run();
