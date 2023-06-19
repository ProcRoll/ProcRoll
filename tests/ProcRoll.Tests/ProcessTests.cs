using System.Diagnostics;
using System.Text;

namespace ProcRoll.Tests;

public class ProcessTests
{
    private Stopwatch sw = new();
    private TimeSpan tmr => sw.Elapsed;

    [SetUp]
    public void Setup()
    {
        sw.Restart();
    }

    [Test(Description = "Use the static Process.Start method to run a file.")]
    public async Task Process_StaticStartFile()
    {
        var outFile = new FileInfo(Path.GetTempFileName());

        try
        {
            TestContext.WriteLine($"{tmr} Writing to {outFile}");
            var process = await Process.Start("dotnet", $"ProcRoll.Tests.Echo.dll \"Success\" --file \"{outFile}\"");
            TestContext.WriteLine($"{tmr} Waiting for write to finish");
            await process.Executing;
            TestContext.WriteLine($"{tmr} Reading file and verifying contents");
            Assert.That((await File.ReadAllTextAsync(outFile.FullName)).TrimEnd(), Is.EqualTo("Success"));
        }
        finally
        {
            TestContext.WriteLine($"{tmr} Deleting {outFile}");
            outFile.Delete();
        }
    }

    [Test(Description = "Use the static Process.Start method with a ProcessStartInfo object to start a process.")]
    public async Task Process_StaticStartInfo()
    {
        var outText = new StringBuilder();
        TestContext.WriteLine($"{tmr} Starting process");
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"ProcRoll.Tests.Echo.dll \"Success\"",
            StdOut = m => outText.Append(m)
        };
        var process = await Process.Start(startInfo);
        TestContext.WriteLine($"{tmr} Waiting for write to finish");
        await process.Executing;
        TestContext.WriteLine($"{tmr} Reading output and verifying contents");
        Assert.That(outText.ToString(), Is.EqualTo("Success"));
    }

    [Test(Description = "Use the static Process.Start method with a ProcessStartInfo object to start a process.")]
    [TestCase(StopMethod.Default)]
    [TestCase(StopMethod.CtrlC)]
    public async Task Process_Stopping(StopMethod stopMethod)
    {
        var outText = new StringBuilder();
        TestContext.WriteLine($"{tmr} Starting process");
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
        TestContext.WriteLine($"{tmr} Stopping process");
        await process.Stop();
        TestContext.WriteLine($"{tmr} Asserting process stopped");
        Assert.That(process.Stopped, Is.True);
    }
}