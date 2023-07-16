using System.Diagnostics;

if (args.Length == 0)
    args = new[] { $"Testing PID {Environment.ProcessId}", "--repeat" };

var message = args[0];
var repeat = args.Contains("--repeat");
var fileArg = Array.IndexOf(args, "--file");

if (fileArg > 0)
{
    File.WriteAllText(args[fileArg + 1], message);
}
else if (repeat)
{
    CancellationTokenSource stoppingTokenSource = new();
    var stoppingToken = stoppingTokenSource.Token;
    Console.CancelKeyPress += (sender, e) =>
    {
        e.Cancel = true;
        stoppingTokenSource.Cancel();
    };

    while (!stoppingToken.IsCancellationRequested)
    {
        Console.WriteLine(message);
        try
        {
            await Task.Delay(1000, stoppingToken);
        }
        catch (TaskCanceledException) { }
    }
}
else
{
    Console.WriteLine(message);
}
