# ProcRoll

A simple way to control external processes from your .NET app.

**Start a process and wait**
```csharp
ProcRoll.Process.Start("ping.exe", "127.0.0.1").Executing.Wait();
```

**Configure from settings file**
```json
  "ProcRoll": {
    "Processes": {
      "Ping": {
        "FileName": "ping.exe",
        "Arguments": "/t 127.0.0.1",
        "StartMode": "Hosted"
      }
    }
  }
```

**Configure in hosting**
```csharp
IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddProcRoll(config =>
        {
            config.Add("Ping", "ping.exe", "/t 127.0.0.1", StartMode.Hosted);
        });
    })
    .Build();
```

## Demos

|  #  | Name | Description |
| --- | --- | --- |
| 1 | [NoHosting](demos/1.NoHosting) | Just start an external process.|
| 2 | [HostedWorkerConfigInFile](demos/2.HostedWorkerConfigInFile) | Start a background worker process that is integrated with the Microsoft.Extensions.Hosting framework. The process is defined in the `appsettings.json` file.
| 3 | [HostedWorkerConfigInCode](demos/3.HostedWorkerConfigInCode) | The same functionality as the previous demo but the started process is configured in `program.cs`.
