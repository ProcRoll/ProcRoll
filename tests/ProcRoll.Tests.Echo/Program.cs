using System.CommandLine;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        var messageArgument = new Argument<string>("message", "Message to write.");
        var repeatOption = new Option<bool?>("--repeat", "Repeat until stopped.");
        var fileOption = new Option<FileInfo?>("--file", "A file to write to instead of the console.");
        var rootCommand = new RootCommand("Echo app for testing ProcRoll.");
        rootCommand.AddArgument(messageArgument);
        rootCommand.AddOption(repeatOption);
        rootCommand.AddOption(fileOption);

        rootCommand.SetHandler(async (context) =>
        {
            var message = context.ParseResult.GetValueForArgument(messageArgument);
            var repeat = context.ParseResult.GetValueForOption(repeatOption);
            var file = context.ParseResult.GetValueForOption(fileOption);

            if (file != null)
            {
                await File.AppendAllTextAsync(file.FullName, message);
            }
            else
            {
                if (repeat is true)
                {
                    var cancellationToken = context.GetCancellationToken();
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine(message);
                        await Task.Delay(1000, cancellationToken);
                    }
                }
                else
                {
                    Console.WriteLine(message);
                }
            }
        });

        return await rootCommand.InvokeAsync(args);
    }
}
