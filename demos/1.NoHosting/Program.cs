using ProcRoll;

await (await Process.Start("dotnet", $"{AppContext.BaseDirectory}ProcRoll.Tests.Echo.dll \"Success\"")).Executing;

var startInfo = new ProcessStartInfo { FileName = "dotnet", Arguments = $"{AppContext.BaseDirectory}ProcRoll.Tests.Echo.dll \"{{message}}\"" };
await (await Process.Start(startInfo, "Substitution 1")).Executing;
await (await Process.Start(startInfo, "Substitution 2")).Executing;
