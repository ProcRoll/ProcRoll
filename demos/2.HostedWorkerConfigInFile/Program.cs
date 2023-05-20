using ProcRoll;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddProcRoll();
    })
    .Build();

host.Run();
