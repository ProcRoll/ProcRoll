using ProcRoll;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddProcRoll(config =>
        {
            config.Add("Echo", "dotnet", "ProcRoll.Tests.Echo.dll \"Success\" --repeat", StartMode.Hosted, StopMethod.CtrlBreak);
        });
    })
    .Build();

host.Run();
