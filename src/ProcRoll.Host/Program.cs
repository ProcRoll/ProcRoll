using ProcRoll;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<ProcessHost>();
    })
    .Build();

host.Run();
