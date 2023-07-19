using ProcRoll;

var startInfo = new ProcessStartInfo { FileName = "dotnet", Arguments = $"{AppContext.BaseDirectory}ProcRoll.Tests.Echo.dll \"This is the message\" --repeat", UseShellExecute = true };

var process = await Process.Run(startInfo);

await Console.Out.WriteLineAsync("Press enter to stop...");
await Console.In.ReadLineAsync();

await process.Stop();
