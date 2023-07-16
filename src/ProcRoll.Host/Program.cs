using ProcRoll;

IHost host = new HostBuilder()
    .ConfigureHostConfiguration(config => config
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Warning"
        })
        .AddCommandLine(args))
    .ConfigureLogging((hostContext, config) => config
        .AddConfiguration(hostContext.Configuration.GetSection("Logging"))
        .AddConsole())
    .ConfigureServices(services =>
    {
        services.AddOptions<HostConfig>().BindConfiguration("Host");
        services.AddOptions<ProcessStartInfo>().BindConfiguration("Process");
        services.AddHostedService<ProcessHost>();
    })
    .Build();

host.Run();
