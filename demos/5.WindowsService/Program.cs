IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<EchoTest>();
    })
    .UseWindowsService(configure => configure.ServiceName = ".NET Joke Service")
    .Build();

host.Run();
