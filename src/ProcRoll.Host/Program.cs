using ProcRoll;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddOptions<HostConfig>().BindConfiguration("Host").ValidateDataAnnotations();
        services.AddOptions<ProcessStartInfo>().BindConfiguration("Process").ValidateDataAnnotations();
        services.AddHostedService<ProcessHost>();
    })
    .Build();

host.Run();
