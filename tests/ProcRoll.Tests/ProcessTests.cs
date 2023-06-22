using System.Diagnostics;
using System.Text;

namespace ProcRoll.Tests;

public class ProcessTests
{
    private readonly Stopwatch sw = new();
    private TimeSpan Time => sw.Elapsed;

    [SetUp]
    public void Setup()
    {
        Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) => e.Cancel = true;
        sw.Restart();
    }

    [Test(Description = "Use the static Process.Start method to run a file.")]
    public async Task Process_StaticStartFile()
    {
        var outFile = new FileInfo(Path.GetTempFileName());

        try
        {
            TestContext.WriteLine($"{Time} Writing to {outFile}");
            var process = await Process.Start("dotnet", $"ProcRoll.Tests.Echo.dll \"Success\" --file \"{outFile}\"");
            TestContext.WriteLine($"{Time} Waiting for write to finish");
            await process.Executing;
            TestContext.WriteLine($"{Time} Reading file and verifying contents");
            Assert.That((await File.ReadAllTextAsync(outFile.FullName)).TrimEnd(), Is.EqualTo("Success"));
        }
        finally
        {
            TestContext.WriteLine($"{Time} Deleting {outFile}");
            outFile.Delete();
        }
    }

    [Test(Description = "Use the static Process.Start method with a ProcessStartInfo object to start a process.")]
    public async Task Process_StaticStartInfo()
    {
        var outText = new StringBuilder();
        TestContext.WriteLine($"{Time} Starting process");
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"ProcRoll.Tests.Echo.dll \"Success\"",
            StdOut = m => outText.Append(m)
        };
        var process = await Process.Start(startInfo);
        TestContext.WriteLine($"{Time} Waiting for write to finish");
        await process.Executing;
        TestContext.WriteLine($"{Time} Reading output and verifying contents");
        Assert.That(outText.ToString(), Is.EqualTo("Success"));
    }

    [Test(Description = "Use the static Process.Start method with a ProcessStartInfo object to start a process.")]
    [TestCase(StopMethod.Default)]
    [TestCase(StopMethod.CtrlC)]
    [TestCase(StopMethod.CtrlBreak)]
    public async Task Process_Stopping(StopMethod stopMethod)
    {
        var outText = new StringBuilder();
        TestContext.WriteLine($"{Time} Starting process");
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"ProcRoll.Tests.Echo.dll \"Success\" --repeat",
            StartedStringMatch = "Success",
            StopMethod = stopMethod,
            StdOut = TestContext.WriteLine
        };
        var process = await Process.Start(startInfo);
        await process.Starting;
        TestContext.WriteLine($"{Time} Stopping process");
        await process.Stop();
        TestContext.WriteLine($"{Time} Asserting process stopped");
        Assert.That(process.Stopped, Is.True);
    }

    private void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        throw new NotImplementedException();
    }
}