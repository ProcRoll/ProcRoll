using EventHandlers;
using ProcRoll;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureProcRoll(configureProcRoll =>
    {
        configureProcRoll.Add<EventHandlerProcess>("Echo1", "dotnet", "ProcRoll.Tests.Echo.dll \"Success 1\" --repeat --usebreak", StartMode.Hosted);
    })
    .Build();

host.Run();
