namespace ProcRoll.Tests;

public class ProcessTests
{
    [Test (Description = "Use the static Process.Start method to run a command.")]
    public async Task Process_StaticStart()
    {
        var outFile = new FileInfo(Path.GetTempFileName());

        try
        {
            var process = Process.Start("cmd.exe", $"/c @echo Success!! > {outFile}");
            await process.Executing;
            Assert.That(File.ReadAllText(outFile.FullName).TrimEnd(), Is.EqualTo($"Success!!"));
        }
        finally
        {
            outFile.Delete();
        }
    }
}