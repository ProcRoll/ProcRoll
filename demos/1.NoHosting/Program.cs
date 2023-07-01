using ProcRoll;

var p1 = await Process.Start("dotnet", $"{AppContext.BaseDirectory}ProcRoll.Tests.Echo.dll \"Success\" --repeat");

//var startInfo = new ProcessStartInfo { FileName = "dotnet", Arguments = $"{AppContext.BaseDirectory}ProcRoll.Tests.Echo.dll \"{{message}}\"", UseShellExecute = true };
//await (await Process.Start(startInfo, "Substitution 1")).Executing;
//await (await Process.Start(startInfo, "Substitution 2")).Executing;

var actions = new ProcessActions
{
    OnStarting = () => Console.WriteLine(">>>OnStarting<<<")
};
var startInfo = new ProcessStartInfo { FileName = "dotnet", Arguments = $"{AppContext.BaseDirectory}ProcRoll.Tests.Echo.dll \"This is the message\" --repeat", UseShellExecute = true };

var p2 = await Process.Start(startInfo, actions);

Task.WaitAll(new[] { p1.Executing, p2.Executing });
