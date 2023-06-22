using ProcRoll;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureProcRoll(configProcRoll =>
    {
        configProcRoll.Add("Echo", "dotnet", "ProcRoll.Tests.Echo.dll \"Success\" --repeat", StartMode.Hosted, StopMethod.CtrlC);
    })
    .Build();

host.Run();
