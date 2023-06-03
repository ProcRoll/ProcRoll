using System.CommandLine;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        var messageArgument = new Argument<string>("message", "Message to write.");
        var fileOption = new Option<FileInfo?>("--file", "A file to write to instead of the console.");
        var rootCommand = new RootCommand("Echo app for testing ProcRoll.");
        rootCommand.AddArgument(messageArgument);
        rootCommand.AddOption(fileOption);

        rootCommand.SetHandler((message, file) =>
        {
            if (file != null)
            {
                File.AppendAllText(file.FullName, message);
            }
            else
            {
                Console.WriteLine(message);
            }
        },
        messageArgument, fileOption);

        return await rootCommand.InvokeAsync(args);
    }
}
