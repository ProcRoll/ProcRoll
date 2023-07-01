using ProcRoll;

namespace EventHandlers;

public class EventHandlerProcess : Process
{
    private readonly ILogger<EventHandlerProcess> logger;

    public EventHandlerProcess(ILogger<EventHandlerProcess> logger, ProcessStartInfo processStartInfo) : base(processStartInfo)
    {
        this.logger = logger;
    }

    //public override Task OnStarting()
    //{
    //    logger.LogInformation("OnStarting");
    //    return Task.CompletedTask;
    //}

    //public override Task OnStarted()
    //{
    //    logger.LogInformation("OnStarted");
    //    return Task.CompletedTask;
    //}

    //public override Task OnStopping()
    //{
    //    logger.LogInformation("OnStopping");
    //    return Task.CompletedTask;
    //}

    //public override Task OnStopped()
    //{
    //    logger.LogInformation("OnStopped");
    //    return Task.CompletedTask;
    //}
}
