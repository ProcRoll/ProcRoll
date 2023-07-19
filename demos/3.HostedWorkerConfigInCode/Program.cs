using ProcRoll;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureProcRoll(configProcRoll =>
    {
        configProcRoll.Add("Echo", "ProcRoll.Tests.Echo.exe", "Success --repeat", StartMode.Hosted, StopMethod.CtrlC);
    })
    .Build();

host.Run();
