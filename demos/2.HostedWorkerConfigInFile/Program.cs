using ProcRoll;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddProcRoll();
    })
    .Build();

host.Run();
