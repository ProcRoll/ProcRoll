using ProcRoll;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddProcRoll(config =>
        {
            config.Add("Ping", "ping.exe", "/t 127.0.0.1", StartMode.Hosted);
        });
    })
    .Build();

host.Run();
